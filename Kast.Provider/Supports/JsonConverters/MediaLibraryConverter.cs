using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kast.Provider.Media
{
    public partial class MediaLibrary
    {
        internal class MediaLibraryConverter : JsonConverter<MediaLibrary>
        {
            private readonly EventHandler<MediaChangeEventArgs>? _onLibraryChangeEventHandler;

            public MediaLibraryConverter(EventHandler<MediaChangeEventArgs>? onLibraryChangeEventHandler = null)
            {
                _onLibraryChangeEventHandler = onLibraryChangeEventHandler;
            }

            public override MediaLibrary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var store = JsonSerializer.Deserialize<IEnumerable<IMedia>>(ref reader, options);
                return store != null
                    ? new MediaLibrary(store, _onLibraryChangeEventHandler)
                    : new MediaLibrary(_onLibraryChangeEventHandler);
            }

            public override void Write(Utf8JsonWriter writer, MediaLibrary value, JsonSerializerOptions options)
                => JsonSerializer.Serialize(writer, value._store.Values, options);
        }
    }
}
