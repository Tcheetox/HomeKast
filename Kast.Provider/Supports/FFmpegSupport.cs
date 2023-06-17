using Xabe.FFmpeg;

namespace Kast.Provider.Supports
{
    public static class FFmpegSupport
    {
        public static void SetExecutable(out string directory)
        {
            directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");
            if (!Directory.Exists(directory))
                throw new ArgumentException($"FFmpeg directory not found {directory}");
            FFmpeg.SetExecutablesPath(directory);
        }
    }
}
