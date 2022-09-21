using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Xabe.FFmpeg.Events;

namespace Cast.Provider.Converter
{
    public class ConversionState
    {
        [JsonIgnore]
        public readonly IMedia Media;
        [JsonIgnore]
        public readonly string TargetPath;
        public string Name => Media.Name;
        public int QueueLength => _queue.Count;
        public Guid Id => Media.Id;
        public string Status => Media.Status.ToString().ToLower();
        public ConversionProgressEventArgs Progress { get; private set; }

        private CancellationTokenSource? _canceller;
        [JsonIgnore]
        public CancellationTokenSource Canceller
            => _canceller ??= new CancellationTokenSource();

        private readonly ConcurrentDictionary<string, ConversionState> _queue;
        public ConversionState(ConcurrentDictionary<string, ConversionState> queue, IMedia media)
        {
            _queue = queue;
            Media = media;

            string targetDirectory = Path.GetDirectoryName(media.LocalPath)!;
            string fileName = "_"
                + Path.GetFileNameWithoutExtension(media.LocalPath)
                + Path.GetExtension(media.ConversionPath);
            TargetPath = Path.Combine(targetDirectory, fileName);
        }

        public void UpdateProgress()
            => Media.Status = ConversionHelper.RequireConversion(Media.Info)
            ? MediaStatus.Unplayable
            : MediaStatus.Playable;

        public void UpdateProgress(ConversionProgressEventArgs progress)
        {
            Progress = progress;
            if (Progress.Percent >= 0)
                Media.Status = MediaStatus.Converting;
            else
                Media.Status = MediaStatus.Queued;
        }
    }
}
