using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Xabe.FFmpeg.Events;
using Cast.Provider.Conversions.Factory;

namespace Cast.Provider.Conversions
{
    public class ConversionState
    {
        public string Name => _media.Name;
        public int QueueLength => _queue.Count;
        public Guid Id => _media.Id;
        public string Status => _media.Status.ToString().ToLower();
        public ConversionProgressEventArgs Progress { get; private set; }
        public FactoryTarget Target { get; private set; }

        private CancellationTokenSource? _canceller;
        [JsonIgnore]
        public CancellationTokenSource Canceller
            => _canceller ??= new CancellationTokenSource();

        private readonly ConcurrentDictionary<Guid, ConversionState> _queue;
        private readonly IMedia _media;

        public ConversionState(ConcurrentDictionary<Guid, ConversionState> queue, IMedia media)
        {
            _queue = queue;
            _media = media;
        }

        public void UpdateProgress() => _media.SetBasicStatus();

        public void UpdateProgress(ConversionProgressEventArgs progress, FactoryTarget target)
        {
            Progress = progress;
            Target = target;
            if (Progress.Percent >= 0)
                _media.Status = MediaStatus.Converting;
            else
                _media.Status = MediaStatus.Queued;
        }
    }
}
