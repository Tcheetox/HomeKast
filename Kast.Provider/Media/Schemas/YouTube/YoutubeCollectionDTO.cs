namespace Kast.Provider.Media.YouTube
{
#pragma warning disable S101 // Types should be named in PascalCase
    internal class YoutubeCollectionDTO
#pragma warning restore S101 // Types should be named in PascalCase
    {
        public string? Kind { get; init; }
        public string? ETag { get; init; }

        private List<YoutubeDTO>? _items;
        public List<YoutubeDTO> Items
        {
            get => _items ??= new List<YoutubeDTO>();
            set => _items = value;
        }
    }
}
