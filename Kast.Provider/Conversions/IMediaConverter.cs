using Kast.Provider.Media;

namespace Kast.Provider.Conversions
{
    public interface IConverter<T> where T : IEquatable<T>
    {
        Task<bool> StartAsync(T media);
        bool Stop(T media);
        bool TryGetValue(T media, out ConversionState? state);
        IEnumerable<ConversionState> GetAll();
    }

    public interface IMediaConverter : IConverter<IMedia>
    { }
}
