using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cast.Provider.Converter;
using Cast.Provider;

namespace Cast.App.Pages
{
    // TODO: add antiforgerytoken
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ConversionModel : PageModel
    {
        private readonly ILogger<ConversionModel> _logger;
        private readonly IMediaConverter _mediaConverter;
        private readonly IMediaProvider _mediaProvider;

        public ConversionModel(ILogger<ConversionModel> logger, IMediaConverter mediaConverter, IMediaProvider providerService)
        {
            _logger = logger;
            _mediaConverter = mediaConverter;
            _mediaProvider = providerService;
        }

        public async Task<IActionResult> OnPostStartConversionAsync(Guid guid)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media != null && media.Status == MediaStatus.Unplayable && _mediaConverter.StartConversion(media))
                return Partial("_MediaFrame", media);
            return BadRequest();
        }

        public async Task<IActionResult> OnPostStopConversionAsync(Guid guid)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media != null && _mediaConverter.StopConvertion(media))
                return Partial("_MediaFrame", media);
            return BadRequest();
        }
    }
}
