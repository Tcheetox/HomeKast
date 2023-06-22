using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Kast.Api.Problems
{
    [JsonConverter(typeof(EmptyProblemDetailsConverter))]
    internal class EmptyProblemDetails : ProblemDetails, IProblemDetails
    {
        public readonly static new EmptyProblemDetails Instance = new();
        private EmptyProblemDetails() 
        { }

        public string? Description { get; set; }
    }
}
