using System;
using Xabe.FFmpeg;

namespace Cast.Provider.Conversions
{
    public static class ConversionExtension
    {
        public static IConversion SetInput(this IConversion conversion, string filePath)
            => conversion.AddParameter($"-i \"{filePath}\"", ParameterPosition.PreInput);

        public static IConversion SetVideoCodec(this IConversion conversion, VideoCodec codec)
            => conversion.AddParameter($"-c:v {codec}");
        public static IConversion SetAudioCodec(this IConversion conversion, AudioCodec codec)
            => conversion.AddParameter($"-c:a {codec}");

        public static IConversion SetAudioStream(this IConversion conversion, ConversionOptions options)
            => conversion.AddParameter($"-map 0:a:{options.AudioStreamIndex}");

        public static IConversion SetVideoStream(this IConversion conversion, ConversionOptions options)
        {
            if (options.BurnSubtitles && options.SubtitlesStreamIndex.HasValue)
                return conversion;
            return conversion.AddParameter("-map 0:v:0");
        }

        public static IConversion SetVideoSize(this IConversion conversion, ConversionOptions options)
        {
            var videoStream = options.Media.Info.VideoStreams.First();
            var optimalSize = (videoStream.Width >= 1920 || videoStream.Height >= 1080)
                ? "1920x1080"
                : "1280x720";
            return conversion.AddParameter($"-s {optimalSize}");
        }

        public static IConversion SetSubtitles(this IConversion conversion, ConversionOptions options)
        {
            if (!options.Media.Info.SubtitleStreams.Any())
                return conversion;

            if (options.BurnSubtitles)
            {
                if (options.SubtitlesStreamIndex.HasValue)
                    conversion.AddParameter($"-filter_complex \"[0:v][0:s:{options.SubtitlesStreamIndex}]overlay[v]\" -map \"[v]\"");
                return conversion;
            }

            for (int i = 0; i < options.Media.Info.SubtitleStreams.Count(); i++)
                conversion.AddParameter($"-map 0:s:{i}");

            return conversion;
        }
    }
}
