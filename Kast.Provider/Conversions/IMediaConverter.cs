﻿using Kast.Provider.Media;

namespace Kast.Provider.Conversions
{
    public interface IConverter<T> where T : IEquatable<T>
    {
        bool Start(T media);
        bool Stop(T media);
        bool TryGetValue(T media, out ConversionContext? state);
        IEnumerable<ConversionContext> GetAll();
    }

    public interface IMediaConverter : IConverter<IMedia>
    { }
}
