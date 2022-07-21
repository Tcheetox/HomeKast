using System;
using Cast.Provider.Converter;
using Cast.Provider.Metadata;
using Xabe.FFmpeg;

namespace Cast.Provider
{
    public enum MediaStatus
    {
        Unknown,
        Playable,
        Unplayable,
        Queued,
        Converting
    }

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
        public Metadata.Metadata Metadata { get; init; }

        public Media UpdateStatus(IMediaConverter mediaConverter)
        {
            if (Info == null)
                Status = MediaStatus.Unknown;
            else if (!MediaConverter.RequireConversion(Info))
                Status = MediaStatus.Playable;
            else if (mediaConverter.TryGetState(this, out ConversionState? state))
                Status = state?.Progress == null ? MediaStatus.Queued : MediaStatus.Converting;
            else
                Status = MediaStatus.Unplayable;
            return this;
        }

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

        public string ConversionPath => Path.Combine(Path.GetTempPath(), Id.ToString());
        #endregion
    }
}
