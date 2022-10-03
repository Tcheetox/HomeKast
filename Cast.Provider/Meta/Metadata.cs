using Newtonsoft.Json;

namespace Cast.Provider.Meta
{
    public class Metadata
    {
        [JsonIgnore]
        public bool HasImage => !string.IsNullOrWhiteSpace(Image);
        [JsonIgnore]
        public string ImageUrl { get; set; }
        [JsonIgnore]
        public string ImagePath { get; set; }
        [JsonIgnore]
        public string? Image => Poster ?? Backdrop;

        public bool? Adult { get; set; }
        [JsonProperty(PropertyName = "backdrop_path")]
        public string Backdrop { get; set; }
        [JsonProperty(PropertyName = "genre_ids")]
        public IEnumerable<int> Genres { get; set; }
        public int? Id { get; set; }
        [JsonProperty(PropertyName = "media_type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "original_language")]
        public string OriginalLanguage { get; set; }
        [JsonProperty(PropertyName = "original_title")]
        public string OriginalTitle { get; set; }
        public string Overview { get; set; }
        public double Popularity { get; set; }
        [JsonProperty(PropertyName = "poster_path")]
        public string Poster { get; set; }
        [JsonProperty(PropertyName = "release_date")]
        public string Released { get; set; }
        public string Title { get; set; }
        public bool? Video { get; set; }
        [JsonProperty(PropertyName = "vote_average")]
        public double VoteAverage { get; set; }
        [JsonProperty(PropertyName = "vote_count")]
        public int VoteCount { get; set; }
    }

    internal class MediaMetadataResults
    {
        public int Page { get; set; }
        [JsonProperty(PropertyName = "total_pages")]
        public int TotalPages { get; set; }
        [JsonProperty(PropertyName = "total_results")]
        public int TotalResults { get; set; }
        public IEnumerable<Metadata> Results { get; set; }
    }
}
