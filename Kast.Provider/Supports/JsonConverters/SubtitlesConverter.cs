using Kast.Provider.Media;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kast.Provider.Supports
{
    internal class SubtitlesListConverter : JsonConverter<SubtitlesList>
    {
        public override SubtitlesList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var subtitles = JsonSerializer.Deserialize<List<Subtitles>>(ref reader, options);
            return new SubtitlesList(subtitles ?? Enumerable.Empty<Subtitles>());
        }

        public override void Write(Utf8JsonWriter writer, SubtitlesList value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, (IList<Subtitles>)value, options);
    }
}
