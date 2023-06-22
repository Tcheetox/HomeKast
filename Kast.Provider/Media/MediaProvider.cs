using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class MediaProvider : IMediaProvider, IDisposable
    {
        protected EventHandler OnLibraryChangeEventHandler;
        protected readonly ILogger<MediaProvider> Logger;
        protected readonly IMetadataProvider MetadataProvider;
        protected readonly SettingsProvider SettingsProvider;

        public MediaProvider(ILogger<MediaProvider> logger, IMetadataProvider metadataProvider, SettingsProvider settingsProvider)
        {
            FFmpegSupport.SetExecutable(out _);

            Logger = logger;
            SettingsProvider = settingsProvider;
            MetadataProvider = metadataProvider;
            OnLibraryChangeEventHandler += (_, __) => _groupedLibrary = null;
        }

        private MediaLibrary? _library;
        public virtual async Task RefreshAsync()
        {
            if (_library != null)
            {
                _library.Dispose();
                _library = null;
            }
            _ = await GetLibraryAsync();
        }

        public async Task<IEnumerable<IMedia>> GetAllAsync() => await GetLibraryAsync();
        public async Task<IMedia?> GetAsync(Guid guid)
        {
            var library = await GetLibraryAsync();
            if (library.TryGetValue(guid, out var media))
                return media;

            return null;
        }

        public async Task<IMedia?> GetAsync(string path)
        {
            var library = await GetLibraryAsync();
            if (library.TryGetValue(path, out IMedia? media))
                return media;

            return null;
        }

        public async Task<bool> AddOrRefreshAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var library = await GetLibraryAsync();
            var state = await library.AddOrRefreshAsync(path, CreateMediaAsync);
            Logger.LogInformation("MediaProvider {state} media from {path}", state ? "added" : "refreshed", path);

            return state;
        }

        public async Task<bool> TryRemoveAsync(string path)
        {
            var library = await GetLibraryAsync();
            if (!library.TryGetValue(path, out IMedia? media))
                return false;

            var state = library.TryRemove(media);
            Logger.LogInformation("MediaProvider {state} {name} ({guid}) from library", state ? "removed" : "could not remove", media!.Name, media.Id);

            return state;
        }

        private object? _groupedLibrary;
        public async Task<IEnumerable<IGrouping<string, T>>> GetGroupAsync<T>(Func<IMedia, T> creator)
        {
            if (_groupedLibrary is IEnumerable<IGrouping<string, T>> group)
                return group;
            
            _groupedLibrary = (await GetAllAsync())
                .Where(m => m.Status != MediaStatus.Hidden)
                .OrderByDescending(m => m.Creation)
                .ThenByDescending(m => m.Status == MediaStatus.Playable)
                .GroupBy(m => m.Name, m => creator(m))
                .ToList();
            
            return (IEnumerable<IGrouping<string, T>>)_groupedLibrary;
        }

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private async Task<MediaLibrary> GetLibraryAsync()
        {
            if (_library != null)
                return _library;

            try
            {
                await _semaphore.WaitAsync();
                if (_library != null)
                    return _library;

                _library = await CreateLibraryAsync();
                Logger.LogInformation("MediaProvider retrieved {media} media from {directories} directories", _library.Count(), SettingsProvider.Library.Directories.Count);
            }
            finally
            {
                if (_semaphore.CurrentCount == 0)
                    _semaphore.Release();
                MassTimer.Print();
            }

            return _library;
        }

        protected virtual async Task<MediaLibrary> CreateLibraryAsync()
        {
            var library = new MediaLibrary(OnLibraryChangeEventHandler);
            await Parallel.ForEachAsync(SettingsProvider
                .Library
                .Directories
                .SelectMany(directory => Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                .Where(f => SettingsProvider.Library.Extensions.Contains(Path.GetExtension(f))),
                new ParallelOptions() { MaxDegreeOfParallelism = SettingsProvider.Application.MaxDegreeOfParallelism },
                async (file, _) => await library.AddOrRefreshAsync(file, CreateMediaAsync));
            return library;
        }

        private static readonly Regex _isSerieRegex = new(@"\bS\d{2}E\d{2}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private async Task<IMedia?> CreateMediaAsync(string file)
        {
            var info = await GetInfoAsync(file);
            if (info == null)
                return null;

            var (normalized, displayed) = Normalization.Names(file);
            var metadata = await MetadataProvider.GetAsync(info, normalized);
            var subtitles = new SubtitlesList(info, SettingsProvider);

            if (Utilities.InsensitiveCompare(metadata.MediaType, "tv") || _isSerieRegex.IsMatch(displayed))
                return new Serie(displayed, info, metadata, subtitles);

            return new Movie(displayed, info, metadata, subtitles);
        }

        public async Task<IMediaInfo?> GetInfoAsync(IMedia media) => await GetInfoAsync(media.FilePath);

        private async Task<IMediaInfo?> GetInfoAsync(string path)
        {
            if (!File.Exists(path))
                return null;

            using (MassTimer.Measure("GetMediaInfo"))
            {
                var canceller = new CancellationTokenSource();
                try
                {
                    canceller.CancelAfter(SettingsProvider.Application.MediaInfoTimeout ?? 3000);
                    var info = await FFmpeg.GetMediaInfo(path, canceller.Token);
                    if (info != null && info.VideoStreams.Any() && info.AudioStreams.Any())
                        return info;
                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogError(ex, "Extracting media info timed-out for {path}", path);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Extracting media info unexpectedly failed for {path}", path);
                }
                finally
                {
                    canceller.Dispose();
                }

                return null;
            }
        }

        #region IDisposable
        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _library != null)
                    _library.Dispose();
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
