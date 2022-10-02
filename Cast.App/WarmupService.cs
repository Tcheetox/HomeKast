using Cast.Provider;

namespace Cast.App
{
    public class WarmupService
    {
        private readonly ILogger<WarmupService> _logger;
        private readonly IMediaProvider _mediaProvider;
        private readonly FileWatcher _fileWatcher;

        public WarmupService(ILogger<WarmupService> logger, IMediaProvider mediaProvider, FileWatcher fileWatcher)
        {
            _logger = logger;
            _mediaProvider = mediaProvider;
            _fileWatcher = fileWatcher;
        }

        public void Warmup()
        {
            _logger.LogInformation($"Warming up {nameof(IMediaProvider)}...");
            _mediaProvider.Warmup();
            _logger.LogInformation($"Warming up {nameof(FileWatcher)}...");
            _fileWatcher.Warmup();
        }
    }
}
