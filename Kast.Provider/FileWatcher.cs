using Microsoft.Extensions.Logging;
using Kast.Provider.Media;
using Kast.Provider.Supports;

namespace Kast.Provider
{
    public class FileWatcher : IRefreshable, IDisposable
    {
        private readonly ILogger _logger;
        private readonly SettingsProvider _settingsProvider;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly IMediaProvider _mediaProvider;

        public FileWatcher(ILogger<FileWatcher> logger, IMediaProvider mediaProvider, SettingsProvider settingsProvider)
        {
            _logger = logger;
            _mediaProvider = mediaProvider;
            _settingsProvider = settingsProvider;
            _settingsProvider.SettingsChanged += OnSettingsChanged;
        }

        /// <summary>
        /// Unbind, dispose and clear watchers
        /// </summary>
        private void SupressWatchers()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Renamed -= Watcher_Changed;
                watcher.Created -= Watcher_Changed;
                watcher.Deleted -= Watcher_Changed;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        public Task RefreshAsync()
        {
            // Dispose and clear existing
            SupressWatchers();

            // Build collection
            foreach (var directory in _settingsProvider
                .Library
                .Directories
                .Where(d => Directory.Exists(d)))
            {
                var watcher = new FileSystemWatcher(directory)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                };
                watcher.Renamed += Watcher_Changed;
                watcher.Created += Watcher_Changed;
                watcher.Deleted += Watcher_Changed;
                _watchers.Add(watcher);
                _logger.LogInformation("File watcher started monitoring {directory}", directory);
            }

            return Task.CompletedTask;
        }

        private void OnSettingsChanged(object? sender, Settings settings) => RefreshAsync();

        private async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.FullPath);
            if (extension.ToLower() == ".vtt")
            {
                foreach (var media in from media in await _mediaProvider.GetAllAsync()
                                      where media.Subtitles
                                        .OfType<Subtitles>()
                                        .Any(s => Utilities.InsensitiveCompare(s.FilePath, e.FullPath))
                                      select media)
                {
                    media.UpdateStatus();
                }
            }
            else if (!_settingsProvider.Library.Extensions.Contains(extension))
                return;

            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    if (await IsFileAvailableAsync(e.FullPath, e))
                        await _mediaProvider.AddOrRefreshAsync(e.FullPath);
                    break;

                case WatcherChangeTypes.Deleted:
                    await _mediaProvider.TryRemoveAsync(e.FullPath);
                    break;

                case WatcherChangeTypes.Renamed:
                    var re = (RenamedEventArgs)e;
                    if (await IsFileAvailableAsync(re.FullPath, e))
                    {
                        await _mediaProvider.TryRemoveAsync(re.OldFullPath);
                        await _mediaProvider.AddOrRefreshAsync(re.FullPath);
                    }
                    break;

                default:
                    break;
            }
        }

        private async Task<bool> IsFileAvailableAsync(string path, FileSystemEventArgs e)
        {
            if (!await IOSupport.IsFileAvailableWithRetryAsync(path, _settingsProvider.Application.FileAccessTimeout))
            {
                _logger.LogWarning("File watcher triggered by a {event} event did not get access to {file}",
                    e.ChangeType.ToString().ToLower(),
                    e.FullPath);
                return false;
            }

            return true;
        }

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
