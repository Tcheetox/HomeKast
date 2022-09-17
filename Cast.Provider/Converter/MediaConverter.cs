using System;
using System.IO;
using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace Cast.Provider.Converter
{
    // TODO: logging
    public class MediaConverter : IMediaConverter
    {
        private readonly ILogger<MediaConverter> _logger;
        private readonly ConversionQueue _conversionQueue;
        private readonly UserProfile _userProfile;

        public IMedia? Current => _conversionQueue.Current;
        public bool HasPendingConversions => _conversionQueue.IsQueueEmpty;

        // Pass-through
        public event EventHandler<ConversionEventArgs> OnMediaConverted
        {
            add => _conversionQueue.OnMediaConverted += value;
            remove => _conversionQueue.OnMediaConverted -= value;
        }

        public MediaConverter(ILogger<MediaConverter> logger, UserProfile userProfile)
        {
            _logger = logger;
            _userProfile = userProfile;
            _conversionQueue = new ConversionQueue(logger);

            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");
            if (!Directory.Exists(directory))
                throw new ArgumentException($"FFmpeg directory not found at {directory}");

            FFmpeg.SetExecutablesPath(directory);
        }

        public async Task<IMediaInfo?> GetMediaInfo(string path, int timeout = 1000)
        {
            var canceller = new CancellationTokenSource();

            try
            {
                canceller.CancelAfter(timeout);
                return await FFmpeg.GetMediaInfo(path, canceller.Token);
            }
            catch (OperationCanceledException ex) 
            {
                _logger.LogError(ex, "Extracting media info timed-out for {path}", path);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Extracting media info unexpectedly failed for {path}", path);
            }
            finally
            {
                canceller.Dispose();
            }

            return null;
        }

        public bool TryGetMediaState(IMedia media, out ConversionState state) => _conversionQueue.TryGet(media, out state);

        public bool StopConvertion(IMedia media)
        {
            if (TryGetMediaState(media, out ConversionState? conversionState) 
                && _conversionQueue.TryRemove(media)
                && !conversionState!.Canceller.IsCancellationRequested)
            {
                conversionState.Canceller.Cancel();
                return true;
            }
            return false;
        }

        public bool StartConversion(IMedia media)
        {
            var (index, subtitles) = GetPreferredConversion(media.Info);

            IVideoStream videoStream = media.Info.VideoStreams
                .First()
                .AddSubtitles(subtitles)
                .SetCodec(VideoCodec.h264)
                .SetOptimalSize();

            IStream audioStream = media.Info.AudioStreams
                .ElementAt(index)
                .SetCodec(AudioCodec.mp3);

            IConversion conversion = FFmpeg.Conversions
                .New()
                .AddStream(audioStream, videoStream)
                .SetOutput(media.ConversionPath);

            return _conversionQueue.TryAdd(media, conversion);
        }

        private (int AudioIndex, IStream? Subtitles) GetPreferredConversion(IMediaInfo info)
        {
            foreach (var preference in _userProfile.Media.Preferences)
            {
                var audioIndex = (info.AudioStreams.FirstOrDefault(audio => audio.Language.ToLower() == preference.Language?.ToLower())?.Index ?? 0) - 1;
                if (audioIndex >= 0 && preference.Subtitles == null)
                    return (audioIndex, null);

                var subtitles = info.SubtitleStreams.FirstOrDefault(subtitles => subtitles.Language.ToLower() == preference.Subtitles?.ToLower());
                if (audioIndex >= 0 && subtitles != null)
                    return (audioIndex, subtitles);
            }

            // Fallback to first audio without subtitles
            return (0, null);
        }
    }
}
