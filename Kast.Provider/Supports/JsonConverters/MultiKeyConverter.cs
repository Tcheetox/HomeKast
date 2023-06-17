using System.Text.Json.Serialization;
using System.Text.Json;

namespace Kast.Provider.Supports
{
    internal class MultiKeyConverter : JsonConverter<MultiKey<Guid, string>>
    {
        public override MultiKey<Guid, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string[] values = reader.GetString()!.Split('|');
            return new MultiKey<Guid, string>(Guid.Parse(values[0]), values[1]);
        }

        public override void Write(Utf8JsonWriter writer, MultiKey<Guid, string> value, JsonSerializerOptions options)
            => writer.WriteStringValue($"{value.Key1}|{value.Key2}");
    }
}
