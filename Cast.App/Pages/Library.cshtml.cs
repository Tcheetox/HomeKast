using Cast.Provider;
using Cast.Provider.Converter;
using Cast.SharedModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;
using System.Net.Mime;

namespace Cast.App.Pages
{
    public class LibraryModel : PageModel
    {
        public IEnumerable<IMedia> Media { get; private set; }
        public Uri Uri { get; private set; }

        public bool HideLayout => HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        private readonly ILogger<LibraryModel> _logger;
        private readonly IProviderService _providerService;
        private readonly IMediaConverter _mediaConverter;
        private readonly UserProfile _userProfile;

        public LibraryModel(ILogger<LibraryModel> logger, IProviderService providerService, IMediaConverter mediaConverter, UserProfile userProfile)
        {
            _logger = logger;
            _providerService = providerService;
            _mediaConverter = mediaConverter;
            _userProfile = userProfile;
        }

        public async Task<IActionResult> OnGet()
        {
            Media = (await _providerService.GetMedia())
                .Select(m => m.Value)
                .OrderBy(m => m.Status != MediaStatus.Playable)
                .ThenBy(m => m.Creation);
            Uri = _userProfile.Application.Uri;
            return Page();
        }

        public async Task<IActionResult> OnGetMedia(Guid guid)
        {
            var media = await _providerService.GetMedia(guid);
            if (media == null)
                return new NoContentResult();

            return Partial("MediaFrame", media);
        }

        // TODO: debug
        public async Task<IActionResult> OnGetMediaConversionProgress(Guid guid)
        {
            var media = await _providerService.GetMedia(guid);
            if (media == null || !_mediaConverter.TryGetState(media, out ConversionState? state))
                return new NoContentResult();

            return new JsonResult(state);
        }

        public async Task<IActionResult> OnGetMediaStream(Guid guid)
        {
            var media = await _providerService.GetMedia(guid);
            if (media == null)
                return new NoContentResult();

            return new PhysicalFileResult(media.LocalPath, "video/mp4")
            {
                EnableRangeProcessing = true
            };
        }
    }
}
