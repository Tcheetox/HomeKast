using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Kast.Provider.Supports;
using Kast.Provider.Extensions;

namespace Kast.Provider.Conversions.Factories
{
    internal class StreamFactory : FactoryBase
    {
        private readonly ILogger<StreamFactory> _logger;
        public StreamFactory(ILogger<StreamFactory> logger, SettingsProvider settingsProvider) : base(settingsProvider, FactoryTarget.Stream)
        {
            _logger = logger;
        }

        public override Func<CancellationToken, Task> ConvertAsync(ConversionContext context)
            => async _token =>
            {
                if (_token.IsCancellationRequested || context.Media.Status != Media.MediaStatus.Unplayable)
                    return;

                // Convert file
                IConversion conversion = FFmpeg.Conversions
                    .New()
                    .SetInput(context.Media.FilePath)
                    .SetVideoCodec(VideoCodec.h264)
                    .SetAudioCodec(AudioCodec.mp3)
                    .SetAudioStream(context.AudioStreamIndex)
                    .SetVideoStream(context)
                    .SetVideoSize(context.Media.Resolution)
                    .SetSubtitles(context)
                    //.UseMultiThread(16)
                    .SetOnProgress((_, args) => context.Update(args, Target))
                    .SetOutputWriter(context.StreamHandle!.WriteAsync, _token);

                _logger.LogInformation("Beginning stream conversion for {media}", context.Media);
                _logger.LogInformation("Arguments: {args}", conversion.Build());

                try
                {
                    await conversion.Start(_token);
                    _logger.LogInformation("Adjusting converted stream for {media}", context.Media);
                    await FFmpeg.Conversions
                        .New()
                        .SetInput(context.TemporaryTargetPath!)
                        .AddParameter($"-c copy -t {context.Media.Info!.Duration.TotalMilliseconds}")
                        .SetOutput(context.TargetPath)
                        .Start(_token);
                    await context.StreamHandle.CompleteAsync();
                }
                finally
                {
                    await IOSupport.DeleteAsync(context.TemporaryTargetPath!, FileAccessTimeout);
                }
            };

    }
}
