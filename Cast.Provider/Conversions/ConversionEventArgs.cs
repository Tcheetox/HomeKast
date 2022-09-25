namespace Cast.Provider.Conversions
{
    public class ConversionEventArgs : EventArgs
    {
        public IMedia Media => Options.Media;
        public readonly ConversionOptions Options;

        public ConversionEventArgs(ConversionOptions options)
        {
            Options = options;
        }
    }
}
