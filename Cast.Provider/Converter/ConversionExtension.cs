using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xabe.FFmpeg;

namespace Cast.Provider.Converter
{
    public static class ConversionExtension
    {
        public static IConversion AddSubtitles(this IConversion conversion, IStream? stream)
            => stream == null ? conversion : conversion.AddStream(stream);

        public static IVideoStream AddSubtitles(this IVideoStream videoStream, IStream? stream)
            => stream == null ? videoStream : videoStream.AddSubtitles(stream.Path);

        public static IVideoStream SetOptimalSize(this IVideoStream videoStream)
        {
            var optimalSize = (videoStream.Width > 1920 || videoStream.Height > 1080)
                ? VideoSize.Hd1080
                : VideoSize.Hd720;
            return videoStream.SetSize(optimalSize);
        }
    }
}
