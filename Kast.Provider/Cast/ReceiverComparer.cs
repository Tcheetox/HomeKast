using System.Diagnostics.CodeAnalysis;
using GoogleCast;

namespace Kast.Provider.Cast
{
    internal class ReceiverComparer : IEqualityComparer<IReceiver>
    {
        public bool Equals(IReceiver? x, IReceiver? y)
        {
            if ((x == null && y == null) || ReferenceEquals(x, y)) 
                return true;

            if (x == null || y == null) return false;

            return x.IPEndPoint.Equals(y.IPEndPoint);
        }

        public int GetHashCode([DisallowNull] IReceiver obj)
            => obj.IPEndPoint.GetHashCode();
    }
}
