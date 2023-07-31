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

                // Define conversion
                IConversion conversion = FFmpeg.Conversions
                    .New()
                    .SetInput(context.Media.FilePath)
                    .SetOutput(context.TemporaryTargetPath)
                    .SetVideoCodec(VideoCodec.h264)
                    .SetAudioCodec(AudioCodec.mp3)
                    .SetAudioStream(context.AudioStreamIndex)
                    .SetVideoStream(context)
                    .SetVideoSize(context.Media.Resolution)
                    .SetSubtitles(context)
                    //.UseMultiThread(16)
                    .AddParameter("-f matroska")
                    .SetOnProgress((_, args) => context.Update(args, Target));

                _logger.LogInformation("Beginning stream conversion for {media}", context.Media);
                _logger.LogInformation("Arguments: {args}", conversion.Build());

                try
                {
                    await conversion.Start(_token);

                    _logger.LogInformation("Adjusting converted stream for {media}", context.Media);

                    await IOSupport.CopyAsync(context.TemporaryTargetPath!, context.TargetPath, timeoutMs: FileAccessTimeout);
                    await context.Handle!.CompleteAsync();
                }
                finally
                {
                    await IOSupport.DeleteAsync(context.TemporaryTargetPath!, FileAccessTimeout);
                }
            };

    }
}
