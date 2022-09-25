using System;
using System.Diagnostics;
using Xabe.FFmpeg;

namespace Cast.Provider.Conversions
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

        public static bool IsConversionRequired(IMediaInfo info)
        {
            var video = info.VideoStreams.First();
            var audio = info.AudioStreams.First();

            return !((video.Width == 1920 && video.Height == 1080 && video.Framerate <= 30)
                || (video.Width == 1280 && video.Height == 720 && video.Framerate <= 60))
                || !IsAcceptedExtension(Path.GetExtension(info.Path))
                || !_videoCodecs.Any(e => e == video.Codec.ToLower())
                || !_audioCodecs.Any(e => e == audio.Codec.ToLower());
        }

        public static void MoveAndRename(string from, string to, int timeout = 10000)
        {
            if (!File.Exists(from))
                return;

            if (File.Exists(to))
                File.Delete(to);

            AccessFileWithRetry(() => File.Move(from, to), timeout);
        }

        private static void AccessFileWithRetry(Action action, int timeout)
        {
            var time = Stopwatch.StartNew();

            while (time.ElapsedMilliseconds < timeout)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException)
                {
                    // No trace needed
                }
            }
        }

        public static bool IsFileAvailableWithRetry(string path, int timeout)
        {
            var time = Stopwatch.StartNew();

            while (time.ElapsedMilliseconds < timeout)
            {
                try
                {
                    using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                    stream.Close();
                    return true;
                }
                catch (IOException)
                {
                    // No trace needed
                }
            }

            return false;
        }
    }
}
