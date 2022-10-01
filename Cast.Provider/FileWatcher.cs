using System;
using Microsoft.Extensions.Logging;
using Cast.Provider.Conversions;
using Cast.SharedModels.User;

namespace Cast.Provider
{
    public class FileWatcher : IDisposable
    {
        private readonly ILogger _logger;
        private readonly UserProfile _userProfile;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly IMediaProvider _mediaProvider;

        public FileWatcher(ILogger<FileWatcher> logger, IMediaProvider mediaProvider, UserProfile userProfile)
        {
            _logger = logger;
            _mediaProvider = mediaProvider;
            _userProfile = userProfile;
            _userProfile.ProfileChanged += OnProfileChanged;
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

        public void Warmup()
        {
            // Dispose and clear existing
            SupressWatchers();

            // Build collection
            foreach (var directory in _userProfile
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
        }

        private void OnProfileChanged(object? sender, EventArgs e) => Warmup();

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.FullPath);
            if (extension.ToLower() == ".vtt")
                _mediaProvider.UpdateMediaSubtitles(e.FullPath);
            else if (!_userProfile.Library.IsMonitoredExtensions(extension))
                return;

            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    if (IsFileAvailable(e.FullPath, e))
                        _mediaProvider.TryAddMedia(e.FullPath);
                    break;

                case WatcherChangeTypes.Deleted:
                    _mediaProvider.TryRemoveMedia(e.FullPath);
                    break;

                case WatcherChangeTypes.Renamed:
                    var re = (RenamedEventArgs)e;
                    if (IsFileAvailable(re.FullPath, e))
                    {
                        _mediaProvider.TryRemoveMedia(re.OldFullPath);
                        _mediaProvider.TryAddMedia(re.FullPath);
                    }
                    break;

                default:
                    break;
            }
        }

        private bool IsFileAvailable(string path, FileSystemEventArgs e)
        {
            if (!ConversionHelper.IsFileAvailableWithRetry(path, 1000))
            {
                _logger.LogWarning("File watcher triggered by a '{event}' event did not get access to {file}",
                    e.ChangeType.ToString().ToLower(),
                    e.FullPath);
                return false;
            }

            return true;
        }

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _userProfile.ProfileChanged -= OnProfileChanged;
                    SupressWatchers();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
