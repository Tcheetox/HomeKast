using System.Text;
using Cast.Provider;
using Cast.SharedModels;
using Cast.SharedModels.User;
using SimplifiedSearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Net.Http.Headers;

namespace Cast.App.Pages
{
    public class LibraryModel : PageModel
    {
        public Uri Uri => _userProfile.Application.Uri;
        public List<IMedia> Library { get; private set; }
        public string MD5 { get; private set; }

        // Hide layout if AJAX request
        public bool HideLayout => HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        private readonly ILogger<LibraryModel> _logger;
        private readonly IMediaProvider _mediaProvider;
        private readonly UserProfile _userProfile;
        private readonly IWebHostEnvironment _appEnvironment;

        public LibraryModel(ILogger<LibraryModel> logger, IMediaProvider mediaProvider, UserProfile userProfile, IWebHostEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _logger = logger;
            _mediaProvider = mediaProvider;
            _userProfile = userProfile;
        }

        public async Task<IActionResult> OnGet(string md5 = "", string query = "")
        {
            var library = (await _mediaProvider.GetAllMedia())
                .Select(m => m.Value)
                .ToList();

            if (!string.IsNullOrWhiteSpace(query))
                library = (await library.SimplifiedSearchAsync(query, x => x.Name)).ToList();

             Library = library
                .OrderByDescending(m => m.Creation)
                .ThenByDescending(m => m.Status == MediaStatus.Playable)
                .ToList();

            MD5 = ComputeLibraryMD5(Library);

            if (!string.IsNullOrWhiteSpace(md5) && md5 == MD5)
                return new NoContentResult();

            _logger.LogDebug("Media library returned with MD5 {MD5}", MD5);
            return Page();
        }

        public async Task<IActionResult> OnGetMediaAsync(Guid guid)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media == null)
                return new NoContentResult();

            return new JsonResult(media);
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

        public async Task<IActionResult> OnGetMediaSubtitles(Guid guid, int idx)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media == null || media.Subtitles.Count <= idx || !media.Subtitles[idx].Exists())
                return new NoContentResult();

            Response.Headers.Add(HeaderNames.AccessControlAllowOrigin, "*");
            return new PhysicalFileResult(media.Subtitles[idx].LocalPath, "text/vtt; charset=utf-8")
            {
                EnableRangeProcessing = true
            };
        }

        public async Task<IActionResult> OnGetMediaPoster(Guid guid)
        {
            var media = await _mediaProvider.GetMedia(guid);
            if (media == null || string.IsNullOrWhiteSpace(media.Metadata?.ImagePath) || !System.IO.File.Exists(media.Metadata.ImagePath))
                return new PhysicalFileResult(Path.Combine(_appEnvironment.WebRootPath, "media", "notfound.png"), "image/png");

            return new PhysicalFileResult(media.Metadata.ImagePath, "image/jpeg");
        }

        #region Private Helper
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
        #endregion
    }
}
