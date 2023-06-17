
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
        public readonly FactoryTarget Target;
        protected readonly SettingsProvider SettingsProvider;
        protected FactoryBase(SettingsProvider settingsProvider, FactoryTarget target)
        {
            SettingsProvider = settingsProvider;
            Target = target;
        }

        public abstract Func<CancellationToken, Task> ConvertAsync(ConversionState state);
    }
}
