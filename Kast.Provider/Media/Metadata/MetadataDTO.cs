using System.Text.Json.Serialization;

namespace Kast.Provider.Media
{
#pragma warning disable S101 // Types should be named in PascalCase
    public class MetadataDTO
#pragma warning restore S101 // Types should be named in PascalCase
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        private const string _baseUrl = "https://image.tmdb.org/t/p/original";
#pragma warning restore S1075 // URIs should not be hardcoded

        private string? _backdrop;
        [JsonPropertyName("backdrop_path")]
        public string? Backdrop
        {
            get => _backdrop;
            set => _backdrop = _baseUrl + value;
        }

        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }
        [JsonPropertyName("popularity")]
        public double? Popularity { get; set; }
        private string? _poster;
        [JsonPropertyName("poster_path")]
        public string? Poster 
        {
            get => _poster; 
            set => _poster = _baseUrl + value;
        }
        [JsonPropertyName("release_date")]
        public string? Released { get; set; }
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("vote_average")]
        public double? Vote { get; set; }
        [JsonPropertyName("vote_count")]
        public int? VoteCount { get; set; }
        [JsonPropertyName("overview")]
        public string? Description { get; set; }
        [JsonPropertyName("genre_ids")]
        public List<int>? Genres { get; set; }
    }
}
