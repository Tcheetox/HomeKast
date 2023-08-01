using Kast.Provider.Conversions;

namespace Kast.Api.Models
{
    public class Conversion
    {
        public static Conversion From(ConversionContext state) => new(state);

        public string Name { get; private set; }
        public Guid Id { get; private set; }
        public string? Target { get; private set; }
        public int Progress { get; private set; }   
        public string? Status { get; private set; }

        private Conversion(ConversionContext state) 
        { 
            Name = state.Name;
            Id = state.Id;
            Target = state.Target?.ToString();
            Progress = state.Progress?.Percent ?? 0;
            Status = state.Status.ToString();
        }
    }
}
