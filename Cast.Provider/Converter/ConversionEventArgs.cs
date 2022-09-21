namespace Cast.Provider.Converter
{
    public class ConversionEventArgs : EventArgs
    {
        public IMedia Media => State.Media;
        public readonly ConversionState State;

        public ConversionEventArgs(ConversionState conversionState)
        {
            State = conversionState;
        }
    }
}
