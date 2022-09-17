
namespace Cast.Provider.Meta
{
    public interface IMetadataProvider
    {
        Task<Metadata> GetMetadataAsync(string lookup);
    }
}