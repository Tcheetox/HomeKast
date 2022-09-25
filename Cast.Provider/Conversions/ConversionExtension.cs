using System;
using Xabe.FFmpeg;
using static Cast.SharedModels.User.Settings;

namespace Cast.Provider.Conversions
{
    public static class ConversionExtension
    {
        public static IConversion AddSubtitles(this IConversion conversion, IEnumerable<ISubtitleStream> subtitleStreams)
        {
            foreach (var subtitles in subtitleStreams)
                conversion.AddStream(subtitles.SetCodec(Xabe.FFmpeg.Streams.SubtitleStream.SubtitleCodec.webvtt));
            return conversion;
        }

        public static IVideoStream SetOptimalSize(this IVideoStream videoStream)
        {
            var optimalSize = (videoStream.Width >= 1920 || videoStream.Height >= 1080)
                ? VideoSize.Hd1080
                : VideoSize.Hd720;
            return videoStream.SetSize(optimalSize);
        }

        public static IAudioStream SetPreferredStream(this IEnumerable<IAudioStream> audioStreams, PreferencesSettings preferences)
        {
            if (preferences?.Language == null)
                return audioStreams.First();

            foreach (var language in preferences.Language)
            {
                var audioStream = audioStreams.FirstOrDefault(audio => audio.Language.ToLower() == language.ToLower());
                if (audioStream != null)
                    return audioStream;
            }

            return audioStreams.First();
        }
    }
}
