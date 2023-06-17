using Microsoft.AspNetCore.Mvc;

namespace Kast.Api.Models
{
    internal class Error
    {
        public readonly static Error BadRequest = new("Bad request");
        public readonly static Error NotFound = new("Not found");
        public static Error Describe(string message) => new(message);
        public string Message { get; private set; }
        private Error(string message)
        {
            Message = message;
        }
    }
}
