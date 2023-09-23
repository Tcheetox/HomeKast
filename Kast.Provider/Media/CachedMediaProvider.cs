using System.Text.Json;
using Microsoft.Extensions.Logging;
using Kast.Provider.Supports;
using static Kast.Provider.Media.MediaLibrary;

namespace Kast.Provider.Media
{
    public class CachedMediaProvider : MediaProviderBase
    {
        private string LibraryPath => Path.Combine(SettingsProvider.Settings.Application.CacheDirectory, "LibCache.json");
        
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly CancellationTokenSource _canceller = new();
       
        private Task? _serializationTask;
        private bool _isStale;

        public CachedMediaProvider(
            ILogger<MediaProviderBase> logger, 
            IMetadataProvider metadataProvider, 
            SettingsProvider settingsProvider, 
            JsonSerializerOptions serializerOptions) 
            : base(logger, metadataProvider, settingsProvider)
        {
            _serializerOptions = new JsonSerializerOptions(serializerOptions);
            _serializerOptions.Converters.Add(new MediaLibraryConverter(OnLibraryChanged));
            _serializerOptions.Converters.Add(new MediaConverter());
            _serializationTask = Task.Run(SaveStaleLibraryAsync, _canceller.Token);
        }

        protected override void OnLibraryChanged(object? sender, MediaChangeEventArgs e)
        {
            base.OnLibraryChanged(sender, e);
            if (e.Event != MediaChangeEventArgs.EventType.CompanionChanged
                && e.Event != MediaChangeEventArgs.EventType.MediaInfoChanged
                && e.Event != MediaChangeEventArgs.EventType.StatusChanged)
                _isStale = true;
        }

        protected override async Task<MediaLibrary> CreateLibraryAsync()
        {
            var library = await RestoreLibraryAsync();
            if (library != null && library.Any())
                return library;

            return await base.CreateLibraryAsync();
        }

        protected override void OnSettingsChanged(object? sender, Settings e)
        {
            if (e.Library.Equals(SettingsProvider.Settings.Library))
                return;

            _ = IOSupport
                .DeleteAsync(LibraryPath, SettingsProvider.Application.FileAccessTimeout)
                .ContinueWith(_ => RefreshAsync());
        }

        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private async Task<MediaLibrary?> RestoreLibraryAsync()
        {
            if (!File.Exists(LibraryPath))
                return null;

            try
            {
                await _fileLock.WaitAsync();
                using var stream = new FileStream(LibraryPath, FileMode.Open);
                var library = await JsonSerializer.DeserializeAsync<MediaLibrary>(stream, _serializerOptions);
                if (library == null)
                    return library;

                foreach (var media in library.Where(m => m.Metadata == null || m.Metadata.HasMissingInfo))
                {
                    var metadata = await MetadataProvider.GetAsync(media);
                    media.UpdateMetadata(metadata);
                }

                return library;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error restoring library from {path}", LibraryPath);
                return null;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async void SaveStaleLibraryAsync()
        {
            while (!_canceller.IsCancellationRequested)
            {
                if (_isStale)
                {
                    await SaveLibraryAsync();
                    _isStale = false;
                }
                await Task.Delay(SettingsProvider.Application.LibrarySerializationInterval, _canceller.Token);
            }
        }

        private async Task SaveLibraryAsync()
        {
            try
            {
                await _fileLock.WaitAsync();

                if (Library == null)
                {
                    await IOSupport.DeleteAsync(LibraryPath, SettingsProvider.Application.FileAccessTimeout);
                    return;
                }

                if (!Library.Any())
                    return;

                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, Library, _serializerOptions);
                stream.Position = 0;

                using var reader = new StreamReader(stream);
                string serialized = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(serialized))
                    return;

                await File.WriteAllTextAsync(LibraryPath, serialized);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving library to {path}", LibraryPath);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!_canceller.IsCancellationRequested)
                {
                    _canceller.Cancel();
                    _canceller.Dispose();
                }
                _serializationTask?.Dispose();
                _serializationTask = null;
            }
        }
        #endregion
    }
}