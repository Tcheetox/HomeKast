using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Kast.Provider.Media;
using Kast.Api.Models;
using Kast.Api.Problems;
using Kast.Provider;

namespace Kast.Api.Controllers
{
    [ApiController]
    [Route("media")]
    public class MediaController : Controller
    {
        private readonly ILogger<MediaController> _logger;
        private readonly IMediaProvider _mediaProvider;

        public MediaController(ILogger<MediaController> logger, IMediaProvider mediaProvider)
        {
            _logger = logger;
            _mediaProvider = mediaProvider;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<IGrouping<string, Media>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAsync()
            => Ok((await _mediaProvider.GetGroupAsync()).Select(MediaGroup.Filtered));

        [HttpGet("{mediaId:guid}")]
        [ProducesResponseType(typeof(Media), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null)
            {
                _logger.LogDebug("Media not found for Id {id}", mediaId);
                return NotFound();
            }

            return Ok(Media.From(media));
        }

        [HttpGet("{mediaId:guid}/image")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RedirectResult), StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetImageAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null || media.Metadata == null) 
                return NoContent();

            Response.Headers.Add(HeaderNames.AccessControlMaxAge, TimeSpan.FromDays(30).TotalSeconds.ToString());
            if (media.Metadata.HasImage)
                return new PhysicalFileResult(media.Metadata.ImagePath!, "image/jpeg");
            if (!string.IsNullOrWhiteSpace(media.Metadata.ImageUrl))
                return Redirect(media.Metadata.ImageUrl);

            return NoContent();
        }

        [HttpGet("{mediaId:guid}/thumbnail")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetThumbnailAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null || media.Metadata == null)
                return NoContent();

            Response.Headers.Add(HeaderNames.AccessControlMaxAge, TimeSpan.FromDays(30).TotalSeconds.ToString());
            if (System.IO.File.Exists(media.Metadata.ThumbnailPath))
                return new PhysicalFileResult(media.Metadata.ThumbnailPath, "image/jpeg");

            return NoContent();
        }

        [HttpGet("{mediaId:guid}/stream")]
        [Produces("video/mp4", "video/x-matroska")]
        [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStreamAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null)
                return NotFound();

            return new PhysicalFileResult(media.FilePath, media.ContentType)
            {
                EnableRangeProcessing = true
            };
        }

        [HttpGet("{mediaId:guid}/subtitles/{idx:int}")]
        [Produces("text/vtt")]
        [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubtitlesAsync([FromRoute] Guid mediaId, [FromRoute] int idx)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            var subtitles = media?.Subtitles.FirstOrDefault(s => s.Index == idx);
            if (subtitles == null)
                return NotFound();

            Response.Headers.Add(HeaderNames.AccessControlAllowOrigin, "*");
            return new PhysicalFileResult(subtitles.FilePath, "text/vtt; charset=utf-8")
            {
                EnableRangeProcessing = true
            };
        }
    }
}
