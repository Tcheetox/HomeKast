using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg.Exceptions;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace Cast.Provider.Conversions.Factory
{
    internal class SubtitlesFactory : FactoryBase
    {
        private readonly ILogger<SubtitlesFactory> _logger;
        public SubtitlesFactory(ILogger<SubtitlesFactory> logger) : base(FactoryTarget.Subtitles)
        {
            _logger = logger;
        }

        public override Task CreateTask(ConversionOptions options, ConversionState state)
            => Task.Run(async () =>
            {
                if (state.Canceller.Token.IsCancellationRequested || !options.Media.Subtitles.Any())
                    return;

                var clock = new Stopwatch();
                clock.Restart();

                StringBuilder command = new($"-i {options.Media.LocalPath}");
                foreach (var subtitle in options.Media.Subtitles)
                {
                    if (File.Exists(subtitle.TemporaryPath))
                        File.Delete(subtitle.TemporaryPath);
                    command.AppendFormat(" -map 0:s:{0} -f webvtt {1}", subtitle.Index, subtitle.TemporaryPath);
                }

                try
                {
                    IConversion conversion = FFmpeg.Conversions
                        .New()
                        .AddParameter(command.ToString());

                    conversion.OnProgress += (object sender, ConversionProgressEventArgs args)
                        => state.UpdateProgress(args, Target);

                    _logger.LogInformation("Beginning subtitles ({count}) extraction for {media.Name} ({media.Id})",
                       options.Media.Subtitles.Count,
                       options.Media.Name,
                       options.Media.Id);

                    await conversion.Start(state.Canceller.Token);

                    // Put converted caption files under user preferred folder
                    foreach (var subtitles in from subtitles in options.Media.Subtitles
                                              where File.Exists(subtitles.TemporaryPath)
                                              select subtitles)
                    {
                        ConversionHelper.MoveAndRename(subtitles.TemporaryPath, subtitles.LocalPath);
                    }

                    clock.Stop();
                    _logger.LogInformation("Extracted {subtitles.count} subtitles stream(s) for {media.Name} ({media.Id}) in {time} ms",
                        options.Media.Subtitles.Count,
                        options.Media.Name,
                        options.Media.Id,
                        clock.Elapsed.Minutes);
                }
                catch (ConversionException ex)
                {
                    _logger.LogError(ex, "Subtitles conversion error for {media.Name} ({media.Id})", options.Media.Name, options.Media.Id);
                }
            }, state.Canceller.Token);
    }
}
