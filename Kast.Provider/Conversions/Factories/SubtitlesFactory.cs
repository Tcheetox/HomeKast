using Kast.Provider.Extensions;
using Kast.Provider.Media;
using Kast.Provider.Supports;
using Microsoft.Extensions.Logging;
using System.Text;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;

namespace Kast.Provider.Conversions.Factories
{
    internal class SubtitlesFactory : FactoryBase
    {
        private readonly ILogger<SubtitlesFactory> _logger;

        public SubtitlesFactory(ILogger<SubtitlesFactory> logger, SettingsProvider settingsProvider) : base(settingsProvider, FactoryTarget.Subtitles)
        {
            _logger = logger;
        }

        public override Func<CancellationToken, Task> ConvertAsync(ConversionContext context)
            => async _token =>
            {
                if (_token.IsCancellationRequested || !context.Media.Subtitles.Any())
                    return;

                StringBuilder command = new($"-i \"{context.Media.FilePath}\"");
                List<KeyValuePair<string, Subtitles>> store = new();
                foreach (var subtitle in context.Media.Subtitles)
                {
                    var tempPath = Path.ChangeExtension(subtitle.FilePath, ".tmp");
                    command.AppendFormat(" -map 0:s:{0} -f webvtt \"{1}\"", subtitle.Index, tempPath);
                    store.Add(new KeyValuePair<string, Subtitles>(tempPath, subtitle));
                }

                IConversion conversion = FFmpeg.Conversions
                    .New()
                    .AddParameter(command.ToString())
                    .SetOnProgress((_, args) => context.Update(args, Target));

                _logger.LogInformation("Beginning ({count}) subtitles extraction for {media}", context.Media.Subtitles.Count, context.Media);
                _logger.LogInformation("Arguments: {args}", conversion.Build());

                try
                {
                    await conversion.Start(_token);

                    // Put converted subtitles files under user preferred folder
                    foreach (var item in store)
                        await IOSupport.MoveAsync(item.Key, item.Value.FilePath, timeoutMs: FileAccessTimeout);
                }
                catch (ConversionException ex)
                {
                    if (context.Type == ConversionContext.ConversionType.SubtitlesOnly)
                        throw;
                    _logger.LogError(ex, "Subtitles conversion error for {media}", context.Media);
                    context.BurnSubtitles = true;
                }
                finally
                {
                    context.Update();
                }
            };
    }
}
