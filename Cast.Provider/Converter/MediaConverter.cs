using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace Cast.Provider.Converter
{
    // TODO: logging
    public class MediaConverter : IMediaConverter
    {
        private readonly ILogger<MediaConverter> _logger;
        private readonly ConversionQueue _conversionQueue;
        public MediaConverter(ILogger<MediaConverter> logger)
        {
            _logger = logger;
            _conversionQueue = new ConversionQueue(logger);

            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");
            if (!Directory.Exists(directory))
                throw new ArgumentException($"!! FFmpeg directory not found at {directory}");

            FFmpeg.SetExecutablesPath(directory);
        }

        public async Task<IMediaInfo?> GetMediaInfo(string path)
        {
            try
            {
                return await FFmpeg.GetMediaInfo(path);
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        public bool TryGetState(IMedia media, out ConversionState? state)
            => _conversionQueue.TryGet(media, out state);

        public bool StartConversion(IMedia media)
        {
            IVideoStream videoStream = media.Info.VideoStreams
                .First()
                .SetCodec(VideoCodec.h264);
            if (videoStream.Width > 1920 || videoStream.Height > 1080)
                videoStream.SetSize(VideoSize.Hd1080);

            IStream audioStream = media.Info.AudioStreams
                .First()
                .SetCodec(AudioCodec.mp3);

            IConversion conversion = FFmpeg.Conversions
                .New()
                .AddStream(audioStream, videoStream)
                .SetOutput(media.ConversionPath);

            return _conversionQueue.TryAdd(media, conversion);
        }

        #region ChromeCast supported formats
        // read more: https://developers.google.com/cast/docs/media
        private static readonly List<string> _acceptedExtensions = new()
        {
            ".mp4",
            ".webm"
        };
        private static readonly List<string> _videoCodecs = new()
        {
            VideoCodec.h264.ToString(),
            VideoCodec.h264_cuvid.ToString(),
            VideoCodec.h264_nvenc.ToString()
        };
        private static readonly List<string> _audioCodecs = new()
        {
            AudioCodec.aac.ToString(),
            AudioCodec.aac_latm.ToString(),
            AudioCodec.mp3.ToString(),
            AudioCodec.mp3adu.ToString(),
            AudioCodec.mp3on4.ToString(),
        };
        public static bool RequireConversion(IMediaInfo info)
        {
            var video = info.VideoStreams.First();
            var audio = info.AudioStreams.First();
            var extension = Path.GetExtension(info.Path).ToLower();

            return !((video.Width == 1920 && video.Height == 1080 && video.Framerate <= 30)
                || (video.Width == 1280 && video.Height == 720 && video.Framerate <= 60))
                || !_acceptedExtensions.Any(e => e == extension)
                || !_videoCodecs.Any(e => e == video.Codec.ToLower())
                || !_audioCodecs.Any(e => e == audio.Codec.ToLower());
        }
        #endregion
    }
}
