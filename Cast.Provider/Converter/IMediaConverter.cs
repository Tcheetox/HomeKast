using Xabe.FFmpeg;

namespace Cast.Provider.Converter
{
    public interface IMediaConverter
    {
        bool StartConversion(IMedia media);
        ConversionState GetCurrentState(IMedia? media);
        bool TryGetState(IMedia media, out ConversionState? state);
        Task<IMediaInfo?> GetMediaInfo(string path);
    }
}