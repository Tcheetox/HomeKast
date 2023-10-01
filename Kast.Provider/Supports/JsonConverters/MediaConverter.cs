using Kast.Provider.Media;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kast.Provider.Supports
{
    internal class MediaConverter : JsonConverter<IMedia>
    {
        private readonly SubtitlesListConverter _subtitlesListConverter = new();

        public override IMedia? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var extraOptions = new JsonSerializerOptions(options);
            extraOptions.Converters.Add(_subtitlesListConverter);

            var document = JsonDocument.ParseValue(ref reader);
            var rootElement = document.RootElement;
            var type = rootElement.GetProperty("Type").GetString();

            return type switch
            {
                "Movie" => JsonSerializer.Deserialize<Movie>(rootElement.GetRawText(), extraOptions),
                "Serie" => JsonSerializer.Deserialize<Serie>(rootElement.GetRawText(), extraOptions),
                _ => throw new JsonException($"Unknown item type: {type}"),
            };
        }

        public override void Write(Utf8JsonWriter writer, IMedia value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case Movie movie:
                    JsonSerializer.Serialize(writer, movie, options);
                    break;
                case Serie serie:
                    JsonSerializer.Serialize(writer, serie, options);
                    break;
                default:
                    throw new JsonException($"Unknown item type: {value.GetType().Name}");
            }
        }
    }
}
