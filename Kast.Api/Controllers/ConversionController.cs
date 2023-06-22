using Microsoft.AspNetCore.Mvc;
using Kast.Provider.Conversions;
using Kast.Provider.Media;
using Kast.Api.Models;
using Kast.Api.Problems;

namespace Kast.Api.Controllers
{
    [ApiController]
    [Route("conversion")]
    public class ConversionController : Controller
    {
        private readonly ILogger<ConversionController> _logger;
        private readonly IMediaConverter _mediaConverter;
        private readonly IMediaProvider _mediaProvider;

        public ConversionController(ILogger<ConversionController> logger, IMediaProvider mediaProvider, IMediaConverter mediaConverter)
        {
            _logger = logger;
            _mediaProvider = mediaProvider;
            _mediaConverter = mediaConverter;
        }

        [HttpGet("{mediaId:guid}/state")]
        [ProducesResponseType(typeof(Conversion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStateAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null)
                return NotFound();

            if (_mediaConverter.TryGetValue(media, out ConversionContext? state))
                return Ok(Conversion.From(state!));

            _logger.LogDebug("No conversion pending");
            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Conversion>), StatusCodes.Status200OK)]
        public IEnumerable<Conversion> Get() 
            => _mediaConverter.GetAll().Select(Conversion.From);

        [HttpPost("{mediaId:guid}/start")]
        [ProducesResponseType(typeof(Conversion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartConversionAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null)
                return BadRequest();

            if (await _mediaConverter.StartAsync(media))
                return StatusCode(StatusCodes.Status201Created);

            if (_mediaConverter.TryGetValue(media, out ConversionContext? state))
                return Ok(Conversion.From(state!));

            return BadRequest();
        }

        [HttpPost("{mediaId:guid}/stop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StopConversionAsync([FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null)
                return NotFound();

            if (!_mediaConverter.Stop(media))
                return BadRequest();

            return Ok();
        }
    }
}
