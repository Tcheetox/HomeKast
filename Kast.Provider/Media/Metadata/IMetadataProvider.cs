
namespace Kast.Provider.Media
{
    public interface IMetadataProvider
    {
        Task<Metadata?> GetAsync(IMedia media);
    }
}
