using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Kast.Api.Problems
{
    [JsonConverter(typeof(EmptyValidationProblemDetailsConverter))]
    internal class EmptyValidationProblemDetails : ValidationProblemDetails, IProblemDetails
    {
        public readonly static new EmptyValidationProblemDetails Instance = new();
        private EmptyValidationProblemDetails() 
        { }

        public string? Description { get; set; }
    }
}
