using System.Text.Json.Serialization;

namespace Kast.Provider.Media.IMDb
{
#pragma warning disable S101 // Types should be named in PascalCase
    internal class MetadataCollectionDTO
#pragma warning restore S101 // Types should be named in PascalCase
    {
        public int Page { get; set; }
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }

        private List<MetadataDTO>? _results;
        public List<MetadataDTO> Results
        {
            get => _results ??= new List<MetadataDTO>();
            set => _results = value;
        }
    }
}
