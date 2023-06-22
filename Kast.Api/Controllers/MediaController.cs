using Microsoft.AspNetCore.Mvc;
using Kast.Provider.Media;
using Kast.Api.Models;
using Microsoft.Net.Http.Headers;
using Kast.Api.Problems;

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
        [ProducesResponseType(typeof(IEnumerable<IGrouping<string, IMedia>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAsync()
            => Ok(await _mediaProvider.GetGroupAsync(Media.From));

        [HttpGet("{mediaId:guid}")]
        [ProducesResponseType(typeof(IMedia), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null)
            {
                _logger.LogDebug("Media not found for Id: {id}", mediaId);
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

            if (media == null || string.IsNullOrWhiteSpace(media.Metadata.Image)) 
                return NoContent();

            Response.Headers.Add(HeaderNames.AccessControlAllowOrigin, "*");
            Response.Headers.Add(HeaderNames.AccessControlMaxAge, TimeSpan.FromDays(30).TotalSeconds.ToString());
            if (media.Metadata.Image.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return Redirect(media.Metadata.Image);
            return new PhysicalFileResult(media.Metadata.Image, "image/jpeg");
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
