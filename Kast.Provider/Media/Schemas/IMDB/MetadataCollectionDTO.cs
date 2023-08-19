using System.Text.Json.Serialization;

namespace Kast.Provider.Media.IMDb
{
    internal class MetadataCollectionDto
    {
        public int Page { get; set; }
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }

        private List<MetadataDto>? _results;
        public List<MetadataDto> Results
        {
            get => _results ??= new List<MetadataDto>();
            set => _results = value;
        }
    }
}
