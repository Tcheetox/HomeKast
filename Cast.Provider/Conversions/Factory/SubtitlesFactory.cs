using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg.Exceptions;
using Xabe.FFmpeg;
using Microsoft.VisualBasic;
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
                List<string> targets = new();
                foreach (var subtitle in options.Media.Subtitles)
                {
                   // string targetPath = subtitle.Path.ChangeExtension(subtitle.LocalPath, ".srt");
                    targets.Add(subtitle.LocalPath);
                    if (File.Exists(subtitle.LocalPath))
                        File.Delete(subtitle.LocalPath);
                    command.AppendFormat(" -map 0:s:{0} -f webvtt {1}", subtitle.Index, subtitle.LocalPath);
                }

                try
                {
                    IConversion conversion = FFmpeg.Conversions
                        .New()
                        .AddParameter(command.ToString());

                    conversion.OnProgress += (object sender, ConversionProgressEventArgs args)
                        => state.UpdateProgress(args, Target);

                    await conversion.Start(state.Canceller.Token);

                    clock.Stop();
                    _logger.LogInformation("Extracted {subtitles.count} subtitles stream(s) for {media.Name} ({media.Id}) in {time} ms",
                        options.Media.Subtitles.Count,
                        options.Media.Name,
                        options.Media.Id,
                        clock.Elapsed.Minutes);
                }
                catch (ConversionException ex)
                {
                    // TODO: nuke temp convert (stream) file as well please
                    foreach (var path in targets.Where(p => File.Exists(p)))
                        File.Delete(path);

                    _logger.LogError(ex, "Subtitles conversion error for {media.Name} ({media.Id})", options.Media.Name, options.Media.Id);
                }
            }, state.Canceller.Token);
    }
}
