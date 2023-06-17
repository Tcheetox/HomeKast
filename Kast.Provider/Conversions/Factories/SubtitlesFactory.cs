using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;
using Xabe.FFmpeg.Exceptions;
using Kast.Provider.Supports;
using Kast.Provider.Media;

namespace Kast.Provider.Conversions.Factories
{
    internal class SubtitlesFactory : FactoryBase
    {
        private readonly ILogger<SubtitlesFactory> _logger;

        public SubtitlesFactory(ILogger<SubtitlesFactory> logger, SettingsProvider settingsProvider) : base(settingsProvider, FactoryTarget.Subtitles)
        {
            _logger = logger;
        }

        public override Func<CancellationToken, Task> ConvertAsync(ConversionState state)
            => async _token =>
            {
                if (_token.IsCancellationRequested || !state.Media.Subtitles.Any())
                    return;

                var clock = Stopwatch.StartNew();
                StringBuilder command = new($"-i \"{state.Media.FilePath}\"");
                List<KeyValuePair<string, Subtitles>> store = new();
                foreach (var subtitle in state.Media.Subtitles)
                {
                    var temp = IOSupport.GetTempPath(".vtt");
                    command.AppendFormat(" -map 0:s:{0} -f webvtt \"{1}\"", subtitle.Index, temp);
                    store.Add(new KeyValuePair<string, Subtitles>(temp, subtitle));
                }

                try
                {
                    IConversion conversion = FFmpeg.Conversions
                        .New()
                        .AddParameter(command.ToString());

                    conversion.OnProgress += (object sender, ConversionProgressEventArgs args) => state.Update(args, Target);
                   
                    _logger.LogInformation("Beginning subtitles ({count}) extraction for {media}", state.Media.Subtitles.Count, state.Media);
                    _logger.LogInformation("Arguments: {args}", conversion.Build());

                    await conversion.Start(_token);

                    // Put converted subtitles files under user preferred folder
                    foreach (var item in store)
                        await IOSupport.MoveAsync(item.Key, item.Value.FilePath, timeoutMs: SettingsProvider.Application.FileAccessTimeout);

                    clock.Stop();
                    _logger.LogInformation("Extracted {count} subtitles stream(s) for {media} in {time} seconds",
                        state.Media.Subtitles.Count,
                        state.Media, 
                        clock.Elapsed.TotalSeconds);
                }
                catch (ConversionException ex)
                {
                    _logger.LogError(ex, "Subtitles conversion error for {media}", state.Media);
                    state.BurnSubtitles = true;
                }
                finally
                {
                    state.Media.UpdateStatus();
                }
            };
    }
}
