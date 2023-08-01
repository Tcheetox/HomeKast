using System.Diagnostics;

namespace Kast.Provider.Conversions
{
    [DebuggerDisplay("{Description}")]
    internal readonly struct ConversionToken : IDisposable
    {
        public readonly IReadOnlyCollection<Func<CancellationToken, Task>> Conversions;
        public readonly EventHandler? OnStart;
        public readonly EventHandler? OnAdd;
        public readonly EventHandler? OnError;
        public readonly EventHandler? OnSuccess;
        public readonly EventHandler? OnFinally;
        public readonly string Description;
        public ConversionToken(
            string description,
            IReadOnlyCollection<Func<CancellationToken, Task>> conversions,
            EventHandler? onStart = null,
            EventHandler? onAdd = null,
            EventHandler? onError = null, 
            EventHandler? onSuccess = null, 
            EventHandler? onFinally = null)
        {
            Description = description;
            Conversions = conversions;
            OnStart = onStart;
            OnAdd = onAdd;
            OnError = onError;
            OnSuccess = onSuccess;
            OnFinally = onFinally;
        }

        public override string ToString() => Description;

        private readonly CancellationTokenSource _canceller = new();
        public bool IsCancellationRequested => _canceller.IsCancellationRequested;
        public CancellationToken CancellationToken => _canceller.Token;
        public void Cancel()
        {
            if (!_canceller.IsCancellationRequested)
                _canceller.Cancel();
        }

        #region IDisposable
        public void Dispose()
        {
            if (!_canceller.IsCancellationRequested)
                _canceller.Cancel();
            _canceller.Dispose();
        }
        #endregion
    }
}