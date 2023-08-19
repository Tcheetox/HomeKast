using System.Text.Json.Serialization;

namespace Kast.Provider.Media.IMDb
{
    internal class MetadataDto
    {
        [JsonPropertyName("backdrop_path")]
        public string? Backdrop { get; init; }
        [JsonPropertyName("media_type")]
        public string? MediaType { get; init; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; init; }
        [JsonPropertyName("original_name")]
        public string? OriginalName { get; init; }
        [JsonPropertyName("popularity")]
        public double? Popularity { get; init; }
        [JsonPropertyName("poster_path")]
        public string? Poster { get; init; }
        [JsonPropertyName("release_date")]
        public string? Released { get; init; }
        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; init; }
        [JsonPropertyName("title")]
        public string? Title { get; init; }
        [JsonPropertyName("vote_average")]
        public double? Vote { get; init; }
        [JsonPropertyName("vote_count")]
        public int? VoteCount { get; init; }
        [JsonPropertyName("overview")]
        public string? Description { get; init; }
        [JsonPropertyName("genre_ids")]
        public List<int>? Genres { get; init; }
    }
}
