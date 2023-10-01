using Kast.Provider.Supports;
using System.Diagnostics;

namespace Kast.Provider.Conversions
{
    public class StreamHandle
    {
        private readonly string _temporaryTargetPath;
        private readonly string _targetPath;

        public async Task BufferingAsync(TimeSpan? timeSpan = null)
        {
            timeSpan ??= TimeSpan.MaxValue;
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed <= timeSpan && (await IOSupport.GetFileInfoAsync(_temporaryTargetPath))?.Length <= Constants.MediaStreamingBuffer)
                await Task.Delay(50);
        }

        public StreamHandle(string temporaryTargetPath, string targetPath)
        {
            _temporaryTargetPath = temporaryTargetPath;
            _targetPath = targetPath;
        }

        public Stream GetReader() => new ReaderStream(this);

        public class ReaderStream : FileStream
        {
            public bool IsReadCompleted => IsWriteCompleted && _readBytes >= TotalBytes;
            private bool IsWriteCompleted => _handle.IsWriteCompleted;

            private FileInfo? _swappedInfo;
            private long TotalBytes
            {
                get
                {
                    if (!IsWriteCompleted)
                        return new FileInfo(_handle._temporaryTargetPath).Length;
                    _swappedInfo ??= new FileInfo(_handle._targetPath);
                    return _swappedInfo.Length;
                }
            }

            private readonly StreamHandle _handle;
            public ReaderStream(StreamHandle handle)
                : base(handle._temporaryTargetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)
            {
                _handle = handle;
            }

            private FileStream? _swappedStream;
            private async ValueTask<int> ReadInternalAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (!IsWriteCompleted)
                    return await base.ReadAsync(buffer, cancellationToken);

                if (_swappedStream == null)
                {
                    _swappedStream = new FileStream(_handle._targetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _swappedStream.Seek(_readBytes, SeekOrigin.Begin);
                }

                return await _swappedStream.ReadAsync(buffer, cancellationToken);
            }

            private long _readBytes;
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                var read = await ReadInternalAsync(buffer, cancellationToken);
                _readBytes += read;
                return read;
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                while (!IsReadCompleted)
                {
                    bufferSize = Math.Min(bufferSize, (int)(TotalBytes - _readBytes));
                    Memory<byte> buffer = new byte[bufferSize];
                    var read = await ReadAsync(buffer, cancellationToken);

                    if (read == 0)
                        continue;

                    if (read < bufferSize) // Trim might be needed when switching the underlying stream
                        buffer = buffer[..read];

                    await destination.WriteAsync(buffer, cancellationToken);
                    await destination.FlushAsync(cancellationToken);
                }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (_swappedStream != null)
                {
                    _swappedStream.Dispose();
                    _swappedStream = null;
                }
            }
        }

        public bool IsWriteCompleted { get; private set; }
        public async Task CompleteAsync()
        {
            if (!IsWriteCompleted)
                return;

            if (!File.Exists(_targetPath))
                throw new ArgumentException("Target file must exist before you flag this handle as completed");

            IsWriteCompleted = true;
            await Task.FromResult(IsWriteCompleted);
        }
    }
}
