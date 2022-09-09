using Xabe.FFmpeg;

namespace Cast.Provider.Converter
{
    public interface IMediaConverter
    {
        bool StartConversion(IMedia media);
        QueueState GetQueueState();
        void StopConvertion(IMedia? media = null);
        bool TryGetMediaState(IMedia media, out ConversionState? state);
        Task<IMediaInfo?> GetMediaInfo(string path);
    }
}