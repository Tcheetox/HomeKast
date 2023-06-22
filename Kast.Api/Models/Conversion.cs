using Kast.Provider.Conversions;
using Kast.Provider.Conversions.Factories;
using Kast.Provider.Media;

namespace Kast.Api.Models
{
    public class Conversion
    {
        public static Conversion From(ConversionContext state) => new(state);

        public string Name { get; private set; }
        public Guid Id { get; private set; }
        public FactoryTarget? Target { get; private set; }
        public int InQueue { get; private set; }
        public int Progress { get; private set; }   
        public MediaStatus Status { get; private set; }

        private Conversion(ConversionContext state) 
        { 
            Name = state.Name;
            Id = state.Id;
            Target = state.Target;
            InQueue = state.QueueCount;
            Progress = state.Progress?.Percent ?? 0;
            Status = state.Status;
        }
    }
}
