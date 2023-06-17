using System.Collections;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class MediaLibrary : IEnumerable<IMedia>
    {
        private readonly MultiConcurrentDictionary<Guid, string, IMedia> _store;
        private readonly CompanionComparer _companionComparer = new();
        private readonly EventHandler? _onChangeEventHandler;

        public MediaLibrary(EventHandler? onChangeEventHandler = null) 
        {
            _onChangeEventHandler = onChangeEventHandler;
            _store = new(key2Comparer: StringComparer.OrdinalIgnoreCase);
        }
        
        public MediaLibrary(MultiConcurrentDictionary<Guid, string, IMedia> store, EventHandler? onChangeEventHandler = null)
        {
            _onChangeEventHandler = onChangeEventHandler;
            _store = store;
        }

        public async Task<bool> AddOrRefreshAsync(string path, Func<string, Task<IMedia?>> creator)
        {
            if (!TryGetValue(path, out IMedia? media))
                media = await creator(path);

            if (media == null)
                return false;

            _store.GetOrAdd(media.Id, media.FilePath, media, out bool added);
            AddCompanionship(media);

            _onChangeEventHandler?.Invoke(this, EventArgs.Empty);

            return added;
        }

        public bool TryRemove(IMedia? media)
        {
            if (media == null) 
                return false;

            if (!_store.TryRemove(media.Id, media.FilePath))
                return false;

            // Remove companionship
            if (media.HasCompanion)
            {
                media.Companion!.Companion = null;
                media.Companion = null;
            }

            _onChangeEventHandler?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public bool TryGetValue(string path, out IMedia? media) => _store.TryGetValue(path, out media);
        public bool TryGetValue(Guid guid, out IMedia? media) => _store.TryGetValue(guid, out media);

        #region IEnumerable<IMedia>
        public IEnumerator<IMedia> GetEnumerator() => _store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Private support
        private void AddCompanionship(IMedia media)
        {
            foreach (var attempt in ExpectedCompanionPath(media))
                if (_store.TryGetValue(attempt, out var companion))
                {
                    media.Companion = companion;
                    companion.Companion = media;
                    return;
                }

            // Slow lookup
            foreach (var companion in this.Where(m => _companionComparer.Equals(m, media)))
            {
                media.Companion = companion;
                companion.Companion = media;
            }
        }

        private static IEnumerable<string> ExpectedCompanionPath(IMedia media)
        {
            if (media.FileName.StartsWith("_"))
            {
                // Long shot a finding the non-converted original media path
                var originalName = media.FileName[1..].Replace(media.Extension, ".mkv");
                yield return Path.Combine(media.Directory, media.FileName[1..]);
                var directoryInfo = new DirectoryInfo(media.Directory);
                if (directoryInfo?.Parent != null)
                    yield return Path.Combine(directoryInfo.Parent.FullName, originalName);
            }

            // Ibidem for the converted file
            var mediaWithoutExtension = media.FileName.Replace(media.Extension, string.Empty);
            var targetDirectory = media.Directory.EndsWith(mediaWithoutExtension) ? media.Directory : Path.Combine(media.Directory, mediaWithoutExtension);
                foreach (var extension in ConversionSupport.AcceptedExtensions)
                    yield return Path.Combine(targetDirectory, $"_{mediaWithoutExtension}{extension}");
        }

        private sealed class CompanionComparer : IEqualityComparer<IMedia>
        {
            public bool Equals(IMedia? x, IMedia? y)
                => x?.Type == y?.Type && x?.Length == y?.Length && Utilities.InsensitiveCompare(x?.Name, y?.Name);

            public int GetHashCode([DisallowNull] IMedia obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + (obj.Type?.GetHashCode() ?? 0);
                    hash = hash * 23 + obj.Name.GetHashCode();
                    hash = hash * 23 + obj.Length.GetHashCode();
                    return hash;
                }
            }
        }
        #endregion

        #region JsonConverter
        internal class Converter : JsonConverter<MediaLibrary>
        {
            private readonly MultiConcurrentDictionaryConverter<Guid, string, IMedia> _multiDictionaryConverter = new(key2Comparer: StringComparer.OrdinalIgnoreCase);
            private readonly MultiKeyConverter _multiKeyConverter = new();
            private readonly MediaConverter _mediaConverter = new();
            private readonly EventHandler? _onLibraryChangeEventHandler;

            public Converter(EventHandler? onLibraryChangeEventHandler = null)
            {
                _onLibraryChangeEventHandler = onLibraryChangeEventHandler;
            }

            private JsonSerializerOptions Extra(JsonSerializerOptions baseOptions)
            {
                var updatedOptions = new JsonSerializerOptions(baseOptions);
                updatedOptions.Converters.Add(_multiKeyConverter);
                updatedOptions.Converters.Add(_mediaConverter);
                return updatedOptions;
            }

            public override MediaLibrary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var store = _multiDictionaryConverter.Read(ref reader, typeToConvert, Extra(options));
                if (store == null)
                    return new MediaLibrary(_onLibraryChangeEventHandler);

                // Build library and restore companionship
                var library = new MediaLibrary(store, _onLibraryChangeEventHandler);
                foreach (var entry in store.Values)
                {
                    if (!File.Exists(entry.FilePath))
                    {
                        library.TryRemove(entry);
                        continue;
                    }

                    if (!entry.HasCompanion)
                        library.AddCompanionship(entry);
                }

                return library;
            }

            public override void Write(Utf8JsonWriter writer, MediaLibrary value, JsonSerializerOptions options)
                => _multiDictionaryConverter.Write(writer, value._store, Extra(options));
        }
        #endregion
    }
}
