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
            SettingsProvider.SettingsChanged += OnSettingsChanged;
            MetadataProvider = metadataProvider;
            OnLibraryChangeEventHandler += (_, __) => _groupedLibrary = null;
        }

        protected virtual void OnSettingsChanged(object? sender, Settings e)
        {
            if (e.Library.Equals(SettingsProvider.Settings.Library))
                return;
            _ = RefreshAsync();
        }

        private MediaLibrary? _library;
        public virtual async Task RefreshAsync()
        {
            if (_library != null)
            {
                _library.Dispose();
                _library = null;
                _groupedLibrary = null;
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

        public async Task<bool> AddOrUpdateAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            var library = await GetLibraryAsync();
            if (library.TryGetValue(path, out IMedia? media))
                return library.AddOrUpdate(media!);

            media = await CreateMediaAsync(path);
            return media != null && library.AddOrUpdate(media);
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

        private IReadOnlyList<IGrouping<string, IMedia>>? _groupedLibrary;
        public async Task<IEnumerable<IGrouping<string, IMedia>>> GetGroupAsync()
            => _groupedLibrary ??= (await GetAllAsync())
            .Where(m => m.Status != MediaStatus.Hidden)
            .OrderByDescending(m => m.FileInfo.CreationTime)
            .GroupBy(m => m.Name)
            .ToList();

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private async Task<MediaLibrary> GetLibraryAsync()
        {
            if (_library != null)
                return _library;

            bool dumpTimer = false;
            try
            {
                await _semaphore.WaitAsync();
                if (_library != null)
                    return _library;

                _library = await CreateLibraryAsync();
                dumpTimer = true;
                Logger.LogInformation("MediaProvider retrieved {media} media from {directories} directories", _library.Count, SettingsProvider.Library.Directories.Count);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Critical error retrieving {me}", nameof(MediaLibrary));
            }
            finally
            {
                _semaphore.Release();
                if (dumpTimer)
                    MassTimer.Print();
            }

            return _library ?? new MediaLibrary(OnLibraryChangeEventHandler);
        }

        protected virtual async Task<MediaLibrary> CreateLibraryAsync()
        {
            var library = new MediaLibrary(OnLibraryChangeEventHandler);
            await Parallel.ForEachAsync(SettingsProvider
                .Library
                .Directories
                .Where(d => Directory.Exists(d))
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
                .Where(f => SettingsProvider.Library.Extensions.Contains(Path.GetExtension(f))),
                new ParallelOptions() { MaxDegreeOfParallelism = SettingsProvider.Application.MaxDegreeOfParallelism },
                async (file, _) =>
                {
                    var media = await CreateMediaAsync(file);
                    if (media != null)
                        library.AddOrUpdate(media);
                });
            return library;
        }

        private static readonly Regex _isSerieRegex = new(@"\bS\d{2}E\d{2,3}\b|\bep\s?(\.|)\s?\d{1,3}\b|(?<!\S)\d{2,3}(?!\S)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private async Task<IMedia?> CreateMediaAsync(string file)
        {
            var info = await GetInfoAsync(file);
            if (info == null)
                return null;

            var subtitles = new SubtitlesList(info, SettingsProvider);
            IMedia media = _isSerieRegex.IsMatch(file)
                ? new Serie(info, subtitles)
                : new Movie(info, subtitles);
            var metadata = await MetadataProvider.GetAsync(media);
            media.UpdateMetadata(metadata);

            return media;
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
                    canceller.CancelAfter(SettingsProvider.Application.MediaInfoTimeout ?? Constants.FileAccessTimeout);
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
