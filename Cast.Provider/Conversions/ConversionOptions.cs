
using System.IO;

namespace Cast.Provider.Conversions
{
    public enum ConversionType
    {
        FullConversion,
        SubtitlesOnly
    }

    public class ConversionOptions
    {
        public ConversionType ConversionType { get; init; }
        public IMedia Media { get; init; }

        private string? _temporaryPath;
        public string TemporaryPath
            => _temporaryPath ??= Path.Combine(Path.GetTempPath(), $"{Media.Id}.mkv");

        private string? _targetPath;
        public string TargetPath
        {
            get
            {
                if (string.IsNullOrEmpty(_targetPath))
                {
                    if (ConversionType == ConversionType.SubtitlesOnly)
                        _targetPath = Media.LocalPath;
                    else
                    {
                        string targetDirectory = Path.GetDirectoryName(Media.LocalPath)!;
                        string fileName = "_"
                            + Path.GetFileNameWithoutExtension(Media.LocalPath)
                            + Path.GetExtension(TemporaryPath);
                        _targetPath = Path.Combine(targetDirectory, fileName);
                    }
                }
                return _targetPath;
            }
        }

        public void DeleteTemporaryFiles()
        {
            try
            {
                foreach (var path in Media.Subtitles.Select(s => s.TemporaryPath).Where(p => File.Exists(p)))
                    File.Delete(path);
                if (File.Exists(TemporaryPath))
                    File.Delete(TemporaryPath);
            }
            catch (Exception)
            {
                // No need to add trace
            }
        }
    }
}
