using Kast.Provider.Media;
using Kast.Provider.Supports;
using Microsoft.Extensions.Logging;

namespace Kast.Provider
{
    public class FileWatcher : IRefreshable, IDisposable
    {
        private const string _default = "default";

        private readonly ILogger _logger;
        private readonly SettingsProvider _settingsProvider;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly IMediaProvider _mediaProvider;
        private readonly object _lock = new();
        private readonly Dictionary<string, Func<FileSystemEventArgs, Task>> _actors;

        public FileWatcher(ILogger<FileWatcher> logger, IMediaProvider mediaProvider, SettingsProvider settingsProvider)
        {
            _logger = logger;
            _mediaProvider = mediaProvider;
            _settingsProvider = settingsProvider;
            _settingsProvider.SettingsChanged += OnSettingsChanged;
            _actors = new Dictionary<string, Func<FileSystemEventArgs, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                { _default, DefaultHandler },
                { Constants.SubtitlesExtension, SubtitlesHandler },
                { string.Empty, DirectoryHandler }
            };
        }

        public Task RefreshAsync()
        {
            lock (_lock)
            {
                // Dispose and clear existing
                SupressWatchers();

                // Build collection
                foreach (var directory in _settingsProvider
                    .Library
                    .Directories
                    .Where(d => Directory.Exists(d)))
                {
                    AddWatcher(directory);
                    _logger.LogInformation("File watcher started monitoring {directory}", directory);
                }

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Unbind, dispose and clear watchers
        /// </summary>
        private void SupressWatchers()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Renamed -= OnWatcherChanged;
                watcher.Created -= OnWatcherChanged;
                watcher.Deleted -= OnWatcherChanged;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        /// <summary>
        /// Add file watcher to directory
        /// </summary>
        /// <param name="directory">Path of interest</param>
        private void AddWatcher(string directory)
        {
            var watcher = new FileSystemWatcher(directory)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };
            watcher.Renamed += OnWatcherChanged;
            watcher.Created += OnWatcherChanged;
            watcher.Deleted += OnWatcherChanged;
            _watchers.Add(watcher);
        }

        private void OnSettingsChanged(object? sender, Settings settings)
        {
            if (settings.Library.Equals(_settingsProvider.Settings.Library))
                return;
            _ = RefreshAsync();
        }

        private async void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.FullPath);
            if (_actors.TryGetValue(extension, out var actor))
                await actor(e);
            else if (_settingsProvider.Library.Extensions.Contains(extension))
                await _actors[_default](e);
        }

        private async Task<bool> IsFileAvailableAsync(string path, FileSystemEventArgs e)
        {
            if (await IOSupport.IsFileAvailableWithRetryAsync(path, _settingsProvider.Application.FileAccessTimeout))
                return true;

            _logger.LogWarning("File watcher triggered by a {event} event did not get access to {file}",
                   e.ChangeType.ToString().ToLower(),
                   e.FullPath);

            return false;
        }

        #region Handlers
        private Func<FileSystemEventArgs, Task> DirectoryHandler
            => async e =>
            {
                var library = await _mediaProvider.GetAllAsync();
                if (e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    var oldDirectory = e.ChangeType == WatcherChangeTypes.Renamed
                        ? ((RenamedEventArgs)e).OldFullPath
                        : e.FullPath;
                    foreach (var path in from path in library
                                         where path.FilePath.StartsWith(oldDirectory)
                                         select path.FilePath)
                    {
                        await _mediaProvider.TryRemoveAsync(path);
                    }

                    if (e.ChangeType == WatcherChangeTypes.Deleted)
                        return;
                }

                await Parallel.ForEachAsync(Directory.EnumerateFiles(e.FullPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => _settingsProvider.Library.Extensions.Contains(Path.GetExtension(f))),
                    new ParallelOptions() { MaxDegreeOfParallelism = _settingsProvider.Application.MaxDegreeOfParallelism },
                    async (file, _) => await _mediaProvider.AddOrUpdateAsync(file));
            };

        private Func<FileSystemEventArgs, Task> SubtitlesHandler
            => async e =>
            {
                foreach (var media in from media in await _mediaProvider.GetAllAsync()
                                      where media.Subtitles.Any(m => m.FilePath == e.FullPath)
                                      select media)
                {
                    media.UpdateStatus();
                }
            };

        private Func<FileSystemEventArgs, Task> DefaultHandler
            => async e =>
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        if (await IsFileAvailableAsync(e.FullPath, e))
                            await _mediaProvider.AddOrUpdateAsync(e.FullPath);
                        break;

                    case WatcherChangeTypes.Deleted:
                        await _mediaProvider.TryRemoveAsync(e.FullPath);
                        break;

                    case WatcherChangeTypes.Renamed:
                        var re = (RenamedEventArgs)e;
                        if (await IsFileAvailableAsync(re.FullPath, e))
                        {
                            await _mediaProvider.TryRemoveAsync(re.OldFullPath);
                            await _mediaProvider.AddOrUpdateAsync(re.FullPath);
                        }
                        break;

                    default:
                        break;
                }
            };
        #endregion

        #region IDisposable
        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _settingsProvider.SettingsChanged -= OnSettingsChanged;
                    SupressWatchers();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
