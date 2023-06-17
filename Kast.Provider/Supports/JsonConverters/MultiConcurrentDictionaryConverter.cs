using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kast.Provider.Supports
{
    internal class MultiConcurrentDictionaryConverter<TK1, TK2, TValue> : JsonConverter<MultiConcurrentDictionary<TK1, TK2, TValue>>
       where TK1 : notnull
       where TK2 : notnull
    {
        private readonly IEqualityComparer<TK1>? _key1Comparer;
        private readonly IEqualityComparer<TK2>? _key2Comparer;
        public MultiConcurrentDictionaryConverter(IEqualityComparer<TK1>? key1Comparer = null, IEqualityComparer<TK2>? key2Comparer = null)
        {
            _key1Comparer = key1Comparer;
            _key2Comparer = key2Comparer;
        }
        public override MultiConcurrentDictionary<TK1, TK2, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Expected start array token but got {reader.TokenType}");

            object? key = null;
            object? value = null;
            List<KeyValuePair<MultiKey<TK1, TK2>, TValue>> store = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString();
                if (propertyName == nameof(KeyValuePair<MultiKey<TK1, TK2>, TValue>.Key))
                {
                    reader.Read();
                    key = JsonSerializer.Deserialize<MultiKey<TK1, TK2>>(ref reader, options);
                }
                else if (propertyName == nameof(KeyValuePair<MultiKey<TK1, TK2>, TValue>.Value))
                {
                    reader.Read();
                    value = JsonSerializer.Deserialize<TValue>(ref reader, options);
                }

                if (key != null && value != null)
                {
                    store.Add(new KeyValuePair<MultiKey<TK1, TK2>, TValue>((MultiKey<TK1, TK2>)key, (TValue)value));
                    key = null;
                    value = null;
                }
            }

            return new MultiConcurrentDictionary<TK1, TK2, TValue>(store, _key1Comparer, _key2Comparer);
        }

        public override void Write(Utf8JsonWriter writer, MultiConcurrentDictionary<TK1, TK2, TValue> value, JsonSerializerOptions options)
        {
            if (value == null)
                return;

            writer.WriteStartArray();
            foreach (var entry in value)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(entry.Key));
                JsonSerializer.Serialize(writer, entry.Key, options);
                writer.WritePropertyName(nameof(entry.Value));
                JsonSerializer.Serialize(writer, entry.Value, options);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
