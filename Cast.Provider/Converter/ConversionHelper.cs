using System;
using System.Diagnostics;
using Xabe.FFmpeg;

namespace Cast.Provider.Converter
{
    internal static class ConversionHelper
    {
        // Read more about supported formats: https://developers.google.com/cast/docs/media
        private static readonly List<string> _acceptedExtensions = new()
        {
            ".mkv",
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

        public static bool IsAcceptedExtension(string extension)
            => _acceptedExtensions.Any(e => e.ToLower() == extension.ToLower());

        public static bool RequireConversion(IMediaInfo info)
        {
            var video = info.VideoStreams.First();
            var audio = info.AudioStreams.First();

            return !((video.Width == 1920 && video.Height == 1080 && video.Framerate <= 30)
                || (video.Width == 1280 && video.Height == 720 && video.Framerate <= 60))
                || !IsAcceptedExtension(Path.GetExtension(info.Path))
                || !_videoCodecs.Any(e => e == video.Codec.ToLower())
                || !_audioCodecs.Any(e => e == audio.Codec.ToLower());
        }

        public static void MoveAndRename(ConversionState state, int timeout = 10000)
        {
            if (!File.Exists(state.Media.ConversionPath))
                return;

            if (File.Exists(state.TargetPath))
                File.Delete(state.TargetPath);

            AccessFileWithRetry(() => File.Move(state.Media.ConversionPath, state.TargetPath), timeout);
        }

        public static void AccessFileWithRetry(Action action, int timeout)
        {
            var time = Stopwatch.StartNew();

            while (time.ElapsedMilliseconds < timeout)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException e)
                {
                    if (e.HResult != -2147024864)
                        throw;
                }
            }

            throw new IOException($"Failed to perform action within {timeout} ms");
        }
    }
}
