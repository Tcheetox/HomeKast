using Microsoft.AspNetCore.Mvc;
using Kast.Provider.Media;
using Kast.Api.Models;

namespace Kast.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
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

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(IMedia), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMediaAsync([FromRoute] Guid id)
        {
            var media = await _mediaProvider.GetAsync(id);
            if (media == null)
            {
                _logger.LogDebug("Media not found for Id: {id}", id);
                return NotFound(Error.NotFound);
            }

            return Ok(Media.From(media));
        }

        [HttpGet("{id:guid}/image")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RedirectResult), StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetMediaImageAsync([FromRoute] Guid id)
        {
            var media = await _mediaProvider.GetAsync(id);

            if (media == null || string.IsNullOrWhiteSpace(media.Metadata.Image)) 
                return NoContent();

            if (media.Metadata.Image.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return Redirect(media.Metadata.Image);
            return new PhysicalFileResult(media.Metadata.Image, "image/jpeg");
        }
    }
}
