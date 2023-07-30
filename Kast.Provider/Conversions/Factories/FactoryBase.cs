
namespace Kast.Provider.Conversions.Factories
{
    public enum FactoryTarget
    {
        None,
        Stream,
        Subtitles
    }

    internal abstract class FactoryBase
    {
        protected readonly FactoryTarget Target;
        protected readonly SettingsProvider SettingsProvider;
        protected int? FileAccessTimeout => SettingsProvider.Application.FileAccessTimeout;

        protected FactoryBase(SettingsProvider settingsProvider, FactoryTarget target)
        {
            SettingsProvider = settingsProvider;
            Target = target;
        }

        public abstract Func<CancellationToken, Task> ConvertAsync(ConversionContext context);
    }
}
