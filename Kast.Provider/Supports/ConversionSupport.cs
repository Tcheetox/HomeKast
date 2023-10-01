using Kast.Provider.Media;
using Xabe.FFmpeg;

namespace Kast.Provider.Supports
{
    public static class ConversionSupport
    {
        // Read more about supported formats: https://developers.google.com/cast/docs/media
        public static readonly IReadOnlySet<string> AcceptedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv",
            ".mp4",
            ".webm"
        };

        public static readonly IReadOnlySet<string> VideoCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VideoCodec.h264.ToString(),
            VideoCodec.h264_cuvid.ToString(),
            VideoCodec.h264_nvenc.ToString()
        };

        public static readonly IReadOnlySet<string> AudioCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AudioCodec.aac.ToString(),
            AudioCodec.aac_latm.ToString(),
            AudioCodec.mp3.ToString(),
            AudioCodec.mp3adu.ToString(),
            AudioCodec.mp3on4.ToString(),
        };

        public static bool IsConversionRequired(MediaBase media)
            => !((media.Resolution == VideoSize.Hd1080 || media.Resolution == VideoSize.Hd720)
            && media.VideoFrameRate <= 60 // TODO: DBC Chromecast spec
            && AcceptedExtensions.Contains(media.FileInfo.Extension)
            && VideoCodecs.Contains(media.VideoCodec)
            && AudioCodecs.Contains(media.AudioCodec));

        public static VideoSize GetResolution(int width, int height)
            => (width, height) switch
            {
                (720, 480) => VideoSize.Ntsc,
                (720, 576) => VideoSize.Pal,
                (352, 240) => VideoSize.NtscFilm,
                (352, 288) => VideoSize.Qpal,
                (768, 576) => VideoSize.Spal,
                (128, 96) => VideoSize.Sqcif,
                (176, 144) => VideoSize.Qcif,
                (704, 576) => VideoSize._4Cif,
                (1408, 1152) => VideoSize._16cif,
                (160, 120) => VideoSize.Qqvga,
                (320, 240) => VideoSize.Qvga,
                (640, 480) => VideoSize.Vga,
                (800, 600) => VideoSize.Svga,
                (1024, 768) => VideoSize.Xga,
                (1600, 1200) => VideoSize.Uxga,
                (2048, 1536) => VideoSize.Qxga,
                (1280, 1024) => VideoSize.Sxga,
                (2560, 2048) => VideoSize.Qsxga,
                (5120, 4096) => VideoSize.Hsxga,
                (1366, 768) => VideoSize.Wxga,
                (1600, 1024) => VideoSize.Wsxga,
                (1920, 1200) => VideoSize.Wuxga,
                (2560, 1600) => VideoSize.Woxga,
                (3200, 2048) => VideoSize.Wqsxga,
                (3840, 2400) => VideoSize.Wquxga,
                (6400, 4096) => VideoSize.Whsxga,
                (7680, 4800) => VideoSize.Whuxga,
                (320, 200) => VideoSize.Cga,
                (640, 350) => VideoSize.Ega,
                (852, 480) => VideoSize.Hd480,
                (1280, 720) => VideoSize.Hd720,
                (1920, 1080) => VideoSize.Hd1080,
                (2048, 1080) => VideoSize._2K,
                (1998, 1080) => VideoSize._2Kflat,
                (2048, 858) => VideoSize._2Kscope,
                (4096, 2160) => VideoSize._4K,
                (3996, 2160) => VideoSize._4Kflat,
                (4096, 1716) => VideoSize._4Kscope,
                (640, 360) => VideoSize.Nhd,
                (240, 160) => VideoSize.Hqvga,
                (400, 240) => VideoSize.Wqvga,
                (432, 240) => VideoSize.Fwqvga,
                (480, 320) => VideoSize.Hvga,
                (960, 540) => VideoSize.Qhd,
                (3840, 2160) => VideoSize.Uhd2160,
                (7680, 4320) => VideoSize.Uhd4320,
                _ => VideoSize.Film
            };
    }
}
