using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kast.Api.Problems
{
    internal class EmptyProblemDetailsConverter : JsonConverter<EmptyProblemDetails>
    {
        public override EmptyProblemDetails? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, EmptyProblemDetails value, JsonSerializerOptions options)
        {
            // Do nothing
        }
    }
}