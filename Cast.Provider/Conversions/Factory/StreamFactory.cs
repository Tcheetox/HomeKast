using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

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
                IVideoStream videoStream = options.Media.Info.VideoStreams
                    .First()
                    .SetCodec(VideoCodec.h264)
                    .SetOptimalSize();

                IStream audioStream = options.Media.Info.AudioStreams
                    .SetPreferredStream(_userProfile.Preferences)
                    .SetCodec(AudioCodec.mp3);

                IConversion conversion = FFmpeg.Conversions
                    .New()
                    .AddStream(audioStream, videoStream)
                    .AddSubtitles(options.Media.Info.SubtitleStreams)
                    .SetOutput(options.TemporaryPath);

                conversion.OnProgress += (object sender, ConversionProgressEventArgs args)
                    => state.UpdateProgress(args, Target);

                await conversion.Start(state.Canceller.Token);

                // Put converted file under user library directory
                ConversionHelper.MoveAndRename(options.TemporaryPath, options.TargetPath);

                clock.Stop();
                _logger.LogInformation("Conversion successful for {media.Name} ({media.Id}) after {time} minutes",
                    options.Media.Name,
                    options.Media.Id,
                    clock.Elapsed.Minutes);

            }, state.Canceller.Token);
    }
}
