using System.Text.Json.Serialization;

namespace Kast.Provider.Media
{
    public class Metadata
    {
        public string? Image { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? Backdrop { get; set; }
        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }
        public double? Popularity { get; set; }
        [JsonPropertyName("poster_path")]
        public string? Poster { get; set; }
        [JsonPropertyName("release_date")]
        public string? Released { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("vote_average")]
        public double? Vote { get; set; }
        [JsonPropertyName("overview")]
        public string? Description { get; set; }
    }

    internal class MetadataResults
    {
        public int Page { get; set; }
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }

        private List<Metadata>? _results;
        public List<Metadata> Results
        {
            get => _results ??= new List<Metadata>();
            set
            {
                if (value == null || !value.Any())
                    return;
                _results = new List<Metadata>() { value.First() };
            }
        }
    }
}
