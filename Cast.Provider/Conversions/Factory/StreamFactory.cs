using System.Diagnostics;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;
using Microsoft.Extensions.Logging;
using Cast.SharedModels.User;

namespace Cast.Provider.Conversions.Factory
{
    internal class StreamFactory : FactoryBase
    {
        private readonly ILogger<StreamFactory> _logger;
        private readonly UserProfile _userProfile;

        public StreamFactory(ILogger<StreamFactory> logger, UserProfile userProfile) : base(FactoryTarget.Stream)
        {
            _logger = logger;
            _userProfile = userProfile;
        }

        public override Task CreateTask(ConversionOptions options, ConversionState state)
            => Task.Run(async () =>
            {
                if (state.Canceller.Token.IsCancellationRequested)
                    return;

                // Nuke existing file
                if (File.Exists(options.TargetPath))
                    File.Delete(options.TargetPath);

                var clock = new Stopwatch();
                clock.Restart();

                // Convert file
                IConversion conversion = FFmpeg.Conversions
                    .New()
                    .SetInput(options.Media.LocalPath)
                    .SetVideoCodec(VideoCodec.h264)
                    .SetAudioCodec(AudioCodec.mp3)
                    .SetAudioStream(options)
                    .SetVideoStream(options)
                    .SetVideoSize(options)
                    .SetSubtitles(options)
                    .SetOutput(options.TemporaryPath);

                conversion.OnProgress += (object sender, ConversionProgressEventArgs args)
                    => state.UpdateProgress(args, Target);

                _logger.LogInformation("Beginning conversion for {Name} ({Id})",
                   options.Media.Name,
                   options.Media.Id);
                _logger.LogInformation("Arguments: {args}", conversion.Build());

                await conversion.Start(state.Canceller.Token);

                // Put converted file under user library directory
                ConversionHelper.MoveAndRename(options.TemporaryPath, options.TargetPath);

                clock.Stop();
                _logger.LogInformation("Conversion successful for {Name} ({Id}) after {time} minutes",
                    options.Media.Name,
                    options.Media.Id,
                    clock.Elapsed.Minutes);

            }, state.Canceller.Token);
    }
}
