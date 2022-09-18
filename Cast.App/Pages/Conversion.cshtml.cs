using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cast.Provider.Converter;
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
