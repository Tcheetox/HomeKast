using System.Diagnostics;

namespace Kast.Provider.Supports
{
    public static class IOSupport
    {
        public static string CreateTargetDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path).TrimStart('_');
            if (!directory.EndsWith(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                directory = Path.Combine(directory, fileNameWithoutExtension);
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static string GetTempPath(string extension = ".temp")
            => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + extension);

        public static async Task MoveAsync(string from, string to, bool overwrite = true, int? timeoutMs = null)
        {
            if (!File.Exists(from))
                return;

            if (File.Exists(to) && (!overwrite || !await TryPerformOnFileWithRetryAsync(to, _to => File.Delete(_to), timeoutMs)))
                return;

            await TryPerformOnFileWithRetryAsync(from, _from => File.Move(from, to), timeoutMs);
        }

        public static async Task DeleteAsync(string path, int? timeoutMs = null)
        {
            if (!File.Exists(path))
                return;

            await TryPerformOnFileWithRetryAsync(path, _path => File.Delete(_path), timeoutMs);
        }

        public static async Task<bool> IsFileAvailableWithRetryAsync(string path, int? timeout = null)
            => await TryPerformOnFileWithRetryAsync(
                path,
                _path =>
                {
                    using FileStream stream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.None);
                    stream.Close();
                },
                timeout);

        private static async Task<bool> TryPerformOnFileWithRetryAsync(string path, Action<string> action, int? timeout)
        {
            var time = Stopwatch.StartNew();
            var write = DateTime.MinValue;
            timeout ??= 5000;

            while (time.ElapsedMilliseconds < timeout)
            {
                try
                {
                    action(path);
                    return true;
                }
                catch (Exception)
                {
                    try
                    {
                        var newWrite = new FileInfo(path).LastWriteTimeUtc;
                        if (write < newWrite)
                        {
                            write = newWrite;
                            time.Restart();
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // No trace needed
                    }
                    await Task.Delay(10);
                }
            }

            return false;
        }
    }
}
