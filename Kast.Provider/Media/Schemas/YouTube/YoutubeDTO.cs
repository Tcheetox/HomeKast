using System.Text.Json.Serialization;

namespace Kast.Provider.Media.YouTube
{
#pragma warning disable S101 // Types should be named in PascalCase
    internal class YoutubeDTO
#pragma warning restore S101 // Types should be named in PascalCase
    {
        public string? Kind { get; init; }
        public string? ETag { get; init; }
        public InternalId? Id { get; init; }

        [JsonIgnore]
        public string? VideoId => Id?.VideoId;

        public class InternalId
        {
            public string? Kind { get; init; }
            public string? VideoId { get; init; }
        }
    }
}
