using System.Text.Json.Serialization;
using Xabe.FFmpeg.Events;

namespace Cast.Provider.Converter
{
    public class ConversionState
    {
        public readonly IMedia SourceMedia;
        public ConversionProgressEventArgs Progress { get; private set; }
        private readonly object _progressLock = new();

        private CancellationTokenSource? _canceller;
        [JsonIgnore]
        public CancellationTokenSource Canceller
        {
            get
            {
                if (_canceller == null)
                    _canceller = new CancellationTokenSource();
                return _canceller;
            }
        }

        public ConversionState(IMedia media)
        {
            SourceMedia = media;
        }

        public void UpdateProgress(ConversionProgressEventArgs progress)
        {
            lock (_progressLock)
            {
                Progress = progress;
                if (progress.Percent == 100)
                    SourceMedia.Status = MediaStatus.Playable;
                else if (progress.Percent > 0)
                    SourceMedia.Status = MediaStatus.Converting;
            }
        }
    }
}
