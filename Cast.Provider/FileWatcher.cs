using System;
using System.Collections.Generic;
using System.Linq;
using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;

namespace Cast.Provider
{
    public class FileWatcher
    {
        // TODO: implement IDisposale
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

        public void Start()
        {
            // Dispose and clear existing
            foreach (var watcher in _watchers)
                watcher.Dispose();
            _watchers.Clear();

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
    }
}
