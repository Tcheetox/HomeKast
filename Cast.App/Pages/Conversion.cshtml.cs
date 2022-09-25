using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cast.Provider.Conversions;
using Cast.Provider;

namespace Cast.App.Pages
{
    public class ConversionModel : PageModel
    {
        private readonly IMediaConverter _mediaConverter;
        private readonly IMediaProvider _mediaProvider;

        public ConversionModel(IMediaConverter mediaConverter, IMediaProvider providerService)
        {
            _mediaConverter = mediaConverter;
            _mediaProvider = providerService;
        }

        public async Task<IActionResult> OnPostStartConversionAsync(Guid guid)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media != null && _mediaConverter.StartConversion(media))
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

        public async Task<IActionResult> OnGetMediaConversionStateAsync(Guid? guid)
        {
            IMedia? media = guid == null ? _mediaConverter.Current : await _mediaProvider.GetMedia(guid.Value);
            if (media == null || !_mediaConverter.TryGetMediaState(media, out ConversionState state))
                return new NoContentResult();

            return new JsonResult(state);
        }
    }
}
