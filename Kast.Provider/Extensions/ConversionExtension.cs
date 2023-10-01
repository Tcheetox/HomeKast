using Kast.Provider.Conversions;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace Kast.Provider.Extensions
{
    public static class ConversionExtension
    {
        public static IConversion SetInput(this IConversion conversion, string filePath)
            => conversion.AddParameter($"-i \"{filePath}\"", ParameterPosition.PreInput);

        public static IConversion SetVideoCodec(this IConversion conversion, VideoCodec codec)
            => conversion.AddParameter($"-c:v {codec}");
        public static IConversion SetAudioCodec(this IConversion conversion, AudioCodec codec)
            => conversion.AddParameter($"-c:a {codec}");

        public static IConversion SetAudioStream(this IConversion conversion, int audioStreamIndex)
            => conversion.AddParameter($"-map 0:a:{audioStreamIndex}");

        public static IConversion SetVideoStream(this IConversion conversion, ConversionContext state)
        {
            if (state.BurnSubtitles && state.StreamIndices.Item3.HasValue)
                return conversion;
            return conversion.AddParameter("-map 0:v:0");
        }

        public static IConversion SetOnProgress(this IConversion conversion, ConversionProgressEventHandler handler)
        {
            conversion.OnProgress += handler;
            return conversion;
        }

        public static IConversion SetOutputWriter(this IConversion conversion, Func<ReadOnlyMemory<byte>, CancellationToken, Task> write, CancellationToken token)
        {
            conversion.AddParameter("-f matroska pipe:1");
            conversion.OnVideoDataReceived += async (_, args) => await write(args.Data, token);
            return conversion;
        }

        public static IConversion SetSubtitles(this IConversion conversion, ConversionContext state)
        {
            if (!state.Media.Info!.SubtitleStreams.Any())
                return conversion;

            if (state.BurnSubtitles)
            {
                if (state.StreamIndices.Item3.HasValue)
                    conversion.AddParameter($"-filter_complex \"[0:v][0:s:{state.StreamIndices.Item3}]overlay[v]\" -map \"[v]\"");
                return conversion;
            }

            for (int i = 0; i < state.Media.Info!.SubtitleStreams.Count(); i++)
                conversion.AddParameter($"-map 0:s:{i}");

            return conversion;
        }

        public static IConversion SetVideoSize(this IConversion conversion, VideoSize resolution)
        {
            var optimalSize = resolution switch
            {
                VideoSize._4K or VideoSize._4Kdci => "4096x2160",
                VideoSize._2K or VideoSize._2Kdci => "2048x1080",
                VideoSize.Hd1080 => "1920x1080",
                VideoSize.Hd720 => "1280x720",
                _ => "1280x720"
            };
            return conversion.AddParameter($"-s {optimalSize}");
        }
    }
}
