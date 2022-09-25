using Xabe.FFmpeg;

namespace Cast.Provider.Conversions
{
    public interface IMediaConverter
    {
        bool StartConversion(IMedia media);
        bool StopConvertion(IMedia media);
        bool TryGetMediaState(IMedia media, out ConversionState state);
        Task<IMediaInfo?> GetMediaInfo(string path, int timeout = 3000);
        IMedia? Current { get; }
        bool HasPendingConversions { get; }
        event EventHandler<ConversionEventArgs> OnMediaConverted;
    }
}