using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public interface IMetadataProvider
    {
        Task<Metadata> GetAsync(IMediaInfo info, string lookup);
    }
}
