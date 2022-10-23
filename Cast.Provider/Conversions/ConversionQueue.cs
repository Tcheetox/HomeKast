using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg.Exceptions;
using Cast.Provider.Conversions.Factory;
using Cast.SharedModels.User;

namespace Cast.Provider.Conversions
{
    internal class ConversionQueue : IDisposable
    {
        private readonly ConcurrentQueue<ConversionOptions> _queue;
        private readonly ConcurrentDictionary<Guid, ConversionState> _conversions;
        private readonly CancellationTokenSource _conversionCanceller;
        private readonly ILogger<ConversionQueue> _logger;
        private readonly SubtitlesFactory _subtitlesFactory;
        private readonly StreamFactory _streamFactory;
        private readonly UserProfile _profile;

        public ConversionQueue(UserProfile userProfile, ILoggerFactory loggerFactory)
        {
            _profile = userProfile;
            _logger = loggerFactory.CreateLogger<ConversionQueue>();
            _streamFactory = new StreamFactory(loggerFactory.CreateLogger<StreamFactory>(), userProfile);
            _subtitlesFactory = new SubtitlesFactory(loggerFactory.CreateLogger<SubtitlesFactory>());
            _queue = new ConcurrentQueue<ConversionOptions>();
            _conversions = new ConcurrentDictionary<Guid, ConversionState>();
            _conversionCanceller = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_conversionCanceller.IsCancellationRequested)
                {
                    if (!_queue.IsEmpty
                    && _queue.TryDequeue(out ConversionOptions? options)
                    && _conversions.TryGetValue(options.Media.Id, out ConversionState? state))
                    {
                        try
                        {
                            _logger.LogWarning("Conversion starting for media: {line} {json}", Environment.NewLine, options.Media);
                            Current = options.Media;
                            options.SetPreferences(userProfile.Preferences);

                            // Extract and assign subtitles
                            await _subtitlesFactory.CreateTask(options, state);

                            // Proceed to media conversion
                            if (options.ConversionType != ConversionType.SubtitlesOnly)
                                await _streamFactory.CreateTask(options, state);

                            // Raise event
                            OnMediaConverted?.Invoke(this, new ConversionEventArgs(options));
                        }
                        catch (OperationCanceledException ex)
                        {
                            options.DeleteTemporaryFiles();
                            _logger.LogError(ex, "Conversion cancelled by user for {Name} ({Id})", options.Media.Name, options.Media.Id);
                        }
                        catch (ConversionException ex)
                        {
                            options.DeleteTemporaryFiles();
                            _logger.LogError(ex, "Conversion error for {Name} ({Id})", options.Media.Name, options.Media.Id);
                        }
                        finally
                        {
                            Current = null;
                            state.UpdateProgress();
                            state.Canceller.Dispose();
                            _conversions.TryRemove(options.Media.Id, out ConversionState _);
                        }
                    }
                    Thread.Sleep(50);
                }
            }, _conversionCanceller.Token);
        }

        #region Public Members
        public IMedia? Current { get; private set; }
        public bool IsQueueEmpty => _conversions.IsEmpty;
        public event EventHandler<ConversionEventArgs> OnMediaConverted;

        public bool TryAdd(ConversionOptions options)
        {
            var state = new ConversionState(_conversions, options.Media);
            if (!_conversions.TryAdd(options.Media.Id, state))
                return false;

            options.Media.UpdateStatus(MediaStatus.Queued);
            _queue.Enqueue(options);

            return true;
        }

        public bool TryGet(IMedia media, out ConversionState state)
            => _conversions.TryGetValue(media.Id, out state!);

        public bool TryRemove(IMedia media)
            => _conversions.TryRemove(media.Id, out _);
        #endregion

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _conversionCanceller.Dispose();
                    foreach (var canceller in _conversions.Select(conversion => conversion.Value.Canceller)
                                                          .Where(canceller => !canceller.IsCancellationRequested))
                        canceller.Cancel();
                    _conversions.Clear();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
