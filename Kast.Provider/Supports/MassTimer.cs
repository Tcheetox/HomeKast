using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Kast.Provider.Supports
{
    public static class MassTimer
    {
        private sealed class Timing : IDisposable
        {
            private long _ticks;
            private int _hits = 1;

            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private readonly ConcurrentDictionary<string, Timing> _store;
            private readonly string _name;
            private readonly object _lock = new();

            public Timing(ConcurrentDictionary<string, Timing> store, string name)
            {
                _store = store;
                _name = name;
            }

            public void Add(Timing timing)
            {
                lock (_lock)
                {
                    _hits++;
                    _ticks += timing._ticks;
                }
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _ticks = _stopwatch.ElapsedTicks;
                _store.AddOrUpdate(_name, this, (_, _timing) =>
                {
                    _timing.Add(this);
                    return _timing;
                });
            }
            public override string ToString()
                => $"   > {_name}: {_hits} hits in {TimeSpan.FromTicks(_ticks).TotalMilliseconds}ms";
        }

        private static ConcurrentDictionary<string, Timing> Store => _lazyStore.Value;
        private static readonly Lazy<ConcurrentDictionary<string, Timing>> _lazyStore
            = new(() => new ConcurrentDictionary<string, Timing>(StringComparer.InvariantCultureIgnoreCase));

        public static IDisposable? Measure(string name)
        {
#if !DEBUG
            return null;
#endif
            return new Timing(Store, name!);
        }

        public static void Print(ILogger? logger = null)
        {
#if !DEBUG
            return;
#endif
            if (!Store.Values.Any())
                return;

            string label = "   > MassTimer stats";
            Debug.WriteLine(label);
            foreach (Timing timing in Store.Values)
                Debug.WriteLine(timing);

            if (logger == null)
            {
                Store.Clear();
                return;
            }

            logger.LogDebug(label);
            foreach (Timing timing in Store.Values)
                logger.LogDebug(timing.ToString());
            Store.Clear();
        }
    }
}
