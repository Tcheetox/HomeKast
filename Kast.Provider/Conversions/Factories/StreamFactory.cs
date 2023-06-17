using System.Diagnostics;
using Xabe.FFmpeg.Events;
using Xabe.FFmpeg;
using Microsoft.Extensions.Logging;
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

        public override Func<CancellationToken, Task> ConvertAsync(ConversionState state)
            => async _token =>
            {
                if (_token.IsCancellationRequested || state.Media.Status != Media.MediaStatus.Unplayable)
                    return;

                await IOSupport.DeleteAsync(state.MediaTargetPath, SettingsProvider.Application.FileAccessTimeout);
                var clock = new Stopwatch();
                clock.Restart();

                // Convert file
                IConversion conversion = FFmpeg.Conversions
                    .New()
                    .SetInput(state.Media.FilePath)
                    .SetVideoCodec(VideoCodec.h264)
                    .SetAudioCodec(AudioCodec.mp3)
                    .SetAudioStream(state.AudioStreamIndex)
                    .SetVideoStream(state)
                    .SetVideoSize(state.Media.Resolution)
                    .SetSubtitles(state)
                    .SetOutput(state.MediaTemporaryPath);

                conversion.OnProgress += (object sender, ConversionProgressEventArgs args) => state.Update(args, Target);

                _logger.LogInformation("Beginning conversion for {media}",
                   state.Media);
                _logger.LogInformation("Arguments: {args}", conversion.Build());

                await conversion.Start(_token);

                // Put converted file under user library directory
                await IOSupport.MoveAsync(state.MediaTemporaryPath, state.MediaTargetPath, timeoutMs: SettingsProvider.Application.FileAccessTimeout);

                clock.Stop();
                _logger.LogInformation("Conversion successful for {media} after {time} minutes",
                    state.Media,
                    clock.Elapsed.TotalMinutes);
            };
    }
}
