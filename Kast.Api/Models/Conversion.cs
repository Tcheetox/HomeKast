using Kast.Provider.Conversions;

namespace Kast.Api.Models
{
    public record class Conversion
    {
        public string? Name { get; private set; }
        public Guid Id { get; private set; }
        public string? Target { get; private set; }
        public int Progress { get; private set; }
        public string? Status { get; private set; }

        public static Conversion From(ConversionContext state)
            => new()
            {
                Name = state.Name,
                Id = state.Id,
                Target = state.Target?.ToString(),
                Progress = state.Progress?.Percent ?? 0,
                Status = state.Status.ToString()
            };
    }
}
