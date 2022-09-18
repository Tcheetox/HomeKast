using System;
using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;

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

        public void Start()
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

        private void OnProfileChanged(object? sender, EventArgs e) => Start();

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.FullPath);
            if (!_userProfile.Library.IsMonitoredExtensions(extension))
                return;

            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    _mediaProvider.TryAddMediaFromPath(e.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                    _mediaProvider.TryRemoveMediaFromPath(e.FullPath);
                    break;
                case WatcherChangeTypes.Renamed:
                    var re = (RenamedEventArgs)e;
                    _mediaProvider.TryRemoveMediaFromPath(re.OldFullPath);
                    _mediaProvider.TryAddMediaFromPath(re.FullPath);
                    break;
                default:
                    break;
            }
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
