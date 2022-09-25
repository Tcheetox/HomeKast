
namespace Cast.Provider.Conversions.Factory
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
        protected FactoryBase(FactoryTarget target)
        {
            Target = target;
        }

        public abstract Task CreateTask(ConversionOptions options, ConversionState state);
    }
}
