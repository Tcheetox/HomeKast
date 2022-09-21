using System.Text;
using Cast.Provider;
using Cast.Provider.Converter;
using Cast.SharedModels;
using Cast.SharedModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cast.App.Pages
{
    public class LibraryModel : PageModel
    {
        public readonly Uri Uri;
        public IEnumerable<IMedia> Library { get; private set; }
        public string MD5 { get; private set; } = string.Empty;

        // Hide layout if AJAX request
        public bool HideLayout => HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        private readonly ILogger<LibraryModel> _logger;
        private readonly IMediaProvider _mediaProvider;
        private readonly IMediaConverter _mediaConverter;

        public LibraryModel(ILogger<LibraryModel> logger, IMediaProvider mediaProvider, IMediaConverter mediaConverter, UserProfile userProfile)
        {
            _logger = logger;
            _mediaProvider = mediaProvider;
            _mediaConverter = mediaConverter;
            Uri = userProfile.Application.Uri;
        }

        public async Task<IActionResult> OnGet(string md5 = "")
        {
            Library = (await _mediaProvider.GetAllMedia())
                .Select(m => m.Value)
                .OrderByDescending(m => m.Creation)
                .ThenByDescending(m => m.Status == MediaStatus.Playable);

            MD5 = ComputeLibraryMD5(Library);

            if (!string.IsNullOrWhiteSpace(md5) && md5 == MD5)
                return new NoContentResult();

            _logger.LogDebug("Media library returned with MD5 {MD5}", MD5);
            return Page();
        }

        private static string ComputeLibraryMD5(IEnumerable<IMedia> library)
        {
            StringBuilder builder = new();
            foreach (var media in library)
            {
                builder.Append(media.Id.ToString());
                builder.Append(media.Status);
            }
            return Helper.ComputeMD5(builder.ToString());
        }

        public async Task<IActionResult> OnGetMediaAsync(Guid guid)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media == null)
                return new NoContentResult();

            return Partial("_MediaFrame", media);
        }

        public async Task<IActionResult> OnGetMediaConversionStateAsync(Guid? guid)
        {
            IMedia? media = guid == null ? _mediaConverter.Current : await _mediaProvider.GetMedia(guid.Value);
            if (media == null || !_mediaConverter.TryGetMediaState(media, out ConversionState state))
                return new NoContentResult();

            return new JsonResult(state);
        }

        public async Task<IActionResult> OnGetMediaStream(Guid guid)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media == null)
                return new NoContentResult();

            _logger.LogInformation("Media library started streaming {media.Name}", media.Name);
            return new PhysicalFileResult(media.LocalPath, "video/mp4")
            {
                EnableRangeProcessing = true
            };
        }
    }
}
