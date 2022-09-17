using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Xabe.FFmpeg.Events;

namespace Cast.Provider.Converter
{
    public class ConversionState
    {
        [JsonIgnore]
        public readonly IMedia SourceMedia;
        [JsonIgnore]
        public readonly string TargetPath;
        public string Name => SourceMedia.Name;
        public int QueueLength => _queue.Count;
        public Guid Id => SourceMedia.Id;
        public string Status => SourceMedia.Status.ToString().ToLower();
        public ConversionProgressEventArgs Progress { get; private set; }

        private CancellationTokenSource? _canceller;
        [JsonIgnore]
        public CancellationTokenSource Canceller
        {
            get
            {
                return _canceller ??= new CancellationTokenSource();
            }
        }

        private readonly ConcurrentDictionary<string, ConversionState> _queue;
        public ConversionState(ConcurrentDictionary<string, ConversionState> queue, IMedia media)
        {
            _queue = queue;
            SourceMedia = media;

            string targetDirectory = Path.GetDirectoryName(media.LocalPath)!;
            string fileName = "_" 
                + Path.GetFileNameWithoutExtension(media.LocalPath)
                + Path.GetExtension(media.ConversionPath);
            TargetPath = Path.Combine(targetDirectory, fileName);
        }

        public void UpdateProgress()
            => SourceMedia.Status = ConversionHelper.RequireConversion(SourceMedia.Info) 
            ? MediaStatus.Unplayable 
            : MediaStatus.Playable;

        public void UpdateProgress(ConversionProgressEventArgs progress)
        {
            Progress = progress;
            if (Progress.Percent >= 0)
                SourceMedia.Status = MediaStatus.Converting;
            else
                SourceMedia.Status = MediaStatus.Queued;
        }
    }
}
