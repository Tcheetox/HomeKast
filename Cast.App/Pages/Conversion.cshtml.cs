using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cast.Provider.Converter;
using Cast.Provider;

namespace Cast.App.Pages
{
    // TODO: debug without net
    // TODO: add antiforgerytoken
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ConversionModel : PageModel
    {
        private readonly ILogger<ConversionModel> _logger;
        private readonly IMediaConverter _converter;
        private readonly IProviderService _providerService;

        public ConversionModel(ILogger<ConversionModel> logger, IMediaConverter mediaConverter, IProviderService providerService)
        {
            _logger = logger;
            _converter = mediaConverter;
            _providerService = providerService;
        }

        public IActionResult OnGetState() => new JsonResult(_converter.GetQueueState());

        public async void OnPostStartConversionAsync(Guid guid)
        {
            var media = await _providerService.GetMedia(guid);
            if (media != null && media.Status == MediaStatus.Unplayable)
                _converter.StartConversion(media);
        }

        public async void OnPostStopConversionAsync(Guid guid)
        {
            var media = await _providerService.GetMedia(guid);
            if (media != null)
                _converter.StopConvertion(media);
        }
    }
}
