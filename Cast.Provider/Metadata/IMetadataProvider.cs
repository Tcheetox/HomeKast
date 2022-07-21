using Cast.Provider.Metadata;

namespace Cast.Provider.MediaInfoProvider
{
    public interface IMetadataProvider
    {
        Task<Metadata.Metadata?> GetMetadataAsync(string lookup);
    }
}