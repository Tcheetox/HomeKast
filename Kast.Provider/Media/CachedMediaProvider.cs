using System.Text.Json;
using Microsoft.Extensions.Logging;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class CachedMediaProvider : MediaProvider
    {
        private string LibraryPath => Path.Combine(SettingsProvider.Settings.Application.CacheDirectory, "LibCache.json");

        private readonly JsonSerializerOptions _serializerOptions;

        public CachedMediaProvider(ILogger<MediaProvider> logger, IMetadataProvider metadataProvider, SettingsProvider settingsProvider, JsonSerializerOptions serializerOptions) 
            : base(logger, metadataProvider, settingsProvider)
        {
            OnLibraryChangeEventHandler += SaveLibraryHandler;

            _serializerOptions = new JsonSerializerOptions(serializerOptions);
            _serializerOptions.Converters.Add(new MediaLibrary.Converter(OnLibraryChangeEventHandler));
        }

        protected override async Task<MediaLibrary> CreateLibraryAsync()
        {
            var library = await RestoreLibraryAsync();
            if (library != null && library.Any())
                return library;

            return await base.CreateLibraryAsync();
        }

        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private async Task<MediaLibrary?> RestoreLibraryAsync()
        {
            try
            {
                if (!File.Exists(LibraryPath))
                    return null;

                await _fileLock.WaitAsync();

                using var stream = new FileStream(LibraryPath, FileMode.Open);
                return await JsonSerializer.DeserializeAsync<MediaLibrary>(stream, _serializerOptions);
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

        private void SaveLibraryHandler(object? sender, EventArgs e)
        {
            if (sender is MediaLibrary library)
                _ = SaveLibraryAsync(library);
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