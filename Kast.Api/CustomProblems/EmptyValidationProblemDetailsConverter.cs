using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kast.Api.Problems
{
    internal class EmptyValidationProblemDetailsConverter : JsonConverter<EmptyValidationProblemDetails>
    {
        public override EmptyValidationProblemDetails? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, EmptyValidationProblemDetails value, JsonSerializerOptions options)
        {
            // Nope
        }
    }
}