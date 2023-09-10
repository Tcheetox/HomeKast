using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public partial class MediaLibrary : IEnumerable<IMedia>, IDisposable
    {
        private readonly MultiConcurrentDictionary<Guid, string, IMedia> _store;
        private readonly CompanionComparer _companionComparer = new();

        private EventHandler? _onChangeEventHandler;

        public int Count => _store.Count;

        public MediaLibrary(EventHandler? onChangeEventHandler = null) 
        {
            _onChangeEventHandler = onChangeEventHandler;
            _store = new(key2Comparer: StringComparer.OrdinalIgnoreCase);
        }
        
        private MediaLibrary(IEnumerable<IMedia> store, EventHandler? onChangeEventHandler = null)
        { 
            _store = new(
                store.Select(e => new KeyValuePair<MultiKey<Guid, string>, IMedia>(new MultiKey<Guid, string>(e.Id, e.FilePath), e)), 
                key2Comparer: StringComparer.OrdinalIgnoreCase
                );

            // Validate entries companionship
            foreach (var entry in _store.Values)
            {
                if (!File.Exists(entry.FilePath))
                {
                    TryRemove(entry);
                    continue;
                }

                if (entry.Companion == null)
                    AddCompanionship(entry);
            }

            // Late binding to prevent events during ctor.
            _onChangeEventHandler = onChangeEventHandler; 
        }

        public bool AddOrUpdate(IMedia media)
        {
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

            RemoveCompanionship(media);

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
        private static void RemoveCompanionship(IMedia media)
        {
            media.Companion?.UpdateCompanion(null);
            media.UpdateCompanion(null);
        }

        private void AddCompanionship(IMedia media)
        {
            foreach (var attempt in ExpectedCompanionPath(media))
                if (_store.TryGetValue(attempt, out var companion))
                {
                    media.UpdateCompanion(companion);
                    companion.UpdateCompanion(media);
                    return;
                }

            // Slow lookup
            foreach (var companion in this.Where(m => _companionComparer.Equals(m, media)))
            {
                media.UpdateCompanion(companion);
                companion.UpdateCompanion(media);
            }
        }

        private static IEnumerable<string> ExpectedCompanionPath(IMedia media)
        {
            if (media.FileInfo.Directory == null)
                yield break;

            if (media.FileName.StartsWith('_'))
            {
                // Long shot at finding the non-converted original media path
                var originalName = media.FileName[1..];
                yield return Path.Combine(media.FileInfo.DirectoryName!, originalName);
                if (!string.IsNullOrWhiteSpace(media.FileInfo.Directory.Parent?.Name))
                    yield return Path.Combine(media.FileInfo.Directory.Parent.Name, originalName);
            }

            // Ibidem for the converted file
            var mediaWithoutExtension = Path.GetFileNameWithoutExtension(media.FilePath);
            var targetDirectory = media.FileInfo.Directory.Name.EndsWith(mediaWithoutExtension)
                ? media.FileInfo.DirectoryName!
                : Path.Combine(media.FileInfo.DirectoryName!, mediaWithoutExtension);
            foreach (var extension in ConversionSupport.AcceptedExtensions)
                yield return Path.Combine(targetDirectory, $"_{mediaWithoutExtension}{extension}");
        }
        #endregion

        #region IEqualityComparer<IMedia>
        private sealed class CompanionComparer : IEqualityComparer<IMedia>
        {
            public bool Equals(IMedia? x, IMedia? y)
                => x?.Type == y?.Type && x?.Length == y?.Length 
                && Utilities.InsensitiveCompare(x?.Name, y?.Name)
                && x?.Id != y?.Id;

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

        #region IDisposable
        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _onChangeEventHandler = null;
                    _store.Clear();
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
