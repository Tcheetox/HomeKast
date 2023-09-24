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

        public static async Task<bool> MoveAsync(string from, string to, bool overwrite = true, int? timeoutMs = null)
        {
            if (!File.Exists(from))
                return false;

            return await TryPerformOnFileWithRetryAsync(from, _from => File.Move(from, to, overwrite), timeoutMs);
        }

        public static async Task<FileInfo?> GetFileInfoAsync(string path, int? timeoutMs = null)
        {
            FileInfo? fileInfo = null;
            await TryPerformOnFileWithRetryAsync(path, path => fileInfo = new FileInfo(path), timeoutMs);
            return fileInfo;
        }

        public static async Task<bool> CopyAsync(string from, string to, bool overwrite = true, int? timeoutMs = null)
        {
            if (!File.Exists(from))
                return false;

            return await TryPerformOnFileWithRetryAsync(from, _from => File.Copy(from, to, overwrite), timeoutMs);
        }

        public static async Task<bool> DeleteAsync(string path, int? timeoutMs = null)
        {
            if (!File.Exists(path))
                return false;

            return await TryPerformOnFileWithRetryAsync(path, _path => File.Delete(_path), timeoutMs);
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
            long length = -1;
            timeout ??= Constants.FileAccessTimeout;

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
                        var info = new FileInfo(path);
                        var newWrite = info.LastWriteTimeUtc;
                        var newLength = info.Length;
                        if (write < newWrite || length < newLength)
                        {
                            write = newWrite;
                            length = newLength;
                            time.Restart();
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // No trace needed
                    }
                    await Task.Delay(30);
                }
            }

            return false;
        }
    }
}
