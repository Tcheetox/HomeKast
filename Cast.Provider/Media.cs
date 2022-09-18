using System;
using System.Diagnostics;
using Cast.Provider.Meta;
using Xabe.FFmpeg;

namespace Cast.Provider
{
    public enum MediaStatus
    {
        Hidden,
        Playable,
        Unplayable,
        Queued,
        Converting
    }

    [DebuggerDisplay("{Name} - {LocalPath}")]
    internal class Media : IMedia
    {
        public string Name { get; init; }
        public string LocalPath { get; init; }
        public DateTime Created { get; init; }
        public long Size { get; init; }
        public TimeSpan Length { get; init; }
        public IMediaInfo Info { get; init; }
        public MediaStatus Status { get; set; }
        public DateTime Creation { get; init; }
        public Metadata Metadata { get; init; }
        public VideoSize Resolution { get; init; }

        #region Conversion members
        private Guid? _conversionId;
        public Guid Id
        {
            get
            {
                if (!_conversionId.HasValue)
                    _conversionId = Guid.NewGuid();
                return _conversionId.Value;
            }
        }

        public string ConversionPath => Path.Combine(Path.GetTempPath(), $"{Id}.mp4");
        #endregion
    }
}
