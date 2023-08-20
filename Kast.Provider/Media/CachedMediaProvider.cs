using System.Text.Json;
using Microsoft.Extensions.Logging;
using Kast.Provider.Supports;
using static Kast.Provider.Media.MediaLibrary;

namespace Kast.Provider.Media
{
    public class CachedMediaProvider : MediaProvider
    {
        private string LibraryPath => Path.Combine(SettingsProvider.Settings.Application.CacheDirectory, "LibCache.json");

        private readonly JsonSerializerOptions _serializerOptions;

        public CachedMediaProvider(ILogger<MediaProvider> logger, IMetadataProvider metadataProvider, SettingsProvider settingsProvider, JsonSerializerOptions serializerOptions) 
            : base(logger, metadataProvider, settingsProvider)
        {
            OnLibraryChangeEventHandler += (_s, _e) => 
            {
                if (_s is MediaLibrary library)
                    _ = SaveLibraryAsync(library);
            };

            _serializerOptions = new JsonSerializerOptions(serializerOptions);
            _serializerOptions.Converters.Add(new MediaLibraryConverter(OnLibraryChangeEventHandler));
            _serializerOptions.Converters.Add(new MediaConverter());
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

        private async Task SaveLibraryAsync(MediaLibrary? library)
        {
            try
            {
                await _fileLock.WaitAsync();

                if (library == null)
                {
                    await IOSupport.DeleteAsync(LibraryPath, SettingsProvider.Application.FileAccessTimeout);
                    return;
                }

                if (!library.Any())
                    return;

                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, library, _serializerOptions);
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
    }
}