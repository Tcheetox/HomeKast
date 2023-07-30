
using Kast.Provider.Supports;

namespace Kast.Provider.Conversions
{
    public class StreamHandle
    {
        private FileStream Writer => _lazyWriter.Value;
        private readonly Lazy<FileStream> _lazyWriter;
        private readonly string _temporaryTargetPath;
        private readonly string _targetPath;
        private readonly TaskCompletionSource _buffering = new();
        public async Task BufferingAsync() => await _buffering.Task;

        public StreamHandle(string temporaryTargetPath, string targetPath) 
        {
            _temporaryTargetPath = temporaryTargetPath;
            _targetPath = targetPath;
            _lazyWriter = new Lazy<FileStream>(() => new FileStream(_temporaryTargetPath, FileMode.Create, FileAccess.Write, FileShare.Read));
        }

        public ReaderStream GetReader() => new(this);

        public class ReaderStream : FileStream
        {
            public bool IsReadCompleted => IsWriteCompleted && _readBytes >= TotalBytes;
            private bool IsWriteCompleted => _handle.IsWriteCompleted;

            private long TotalBytes => _handle._writtenBytes;

            private readonly StreamHandle _handle;
            public ReaderStream(StreamHandle handle) 
                : base(handle._temporaryTargetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)
            {
                _handle = handle;
            }

            private FileStream? _swappedStream;
            private async ValueTask<int> ReadInternalAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (!IsWriteCompleted || !File.Exists(_handle._targetPath))
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

        private long _writtenBytes;
        public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Writer.WriteAsync(buffer, cancellationToken);
            await Writer.FlushAsync(cancellationToken);
            _writtenBytes += buffer.Length;
            if (_writtenBytes >= Constants.MediaStreamingBuffer && !_buffering.Task.IsCompleted)
                _buffering.TrySetResult();
        }

        public bool IsWriteCompleted { get; private set; }
        public async Task CompleteAsync()
        {
            if (!_lazyWriter.IsValueCreated || IsWriteCompleted)
                return;

            await Writer.FlushAsync();
            await Writer.DisposeAsync();
            IsWriteCompleted = true;
        }
    }
}
