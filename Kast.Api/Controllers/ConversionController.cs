using Microsoft.AspNetCore.Mvc;
using Kast.Provider.Conversions;
using Kast.Provider.Media;
using Kast.Api.Models;

namespace Kast.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
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

        [HttpGet("{id:guid}/state")]
        [ProducesResponseType(typeof(Conversion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStateAsync([FromRoute] Guid id)
        {
            var media = await _mediaProvider.GetAsync(id);
            if (media == null)
                return NotFound(Error.NotFound);

            if (_mediaConverter.TryGetValue(media, out ConversionState? state))
                return Ok(Conversion.From(state!));

            _logger.LogDebug("No conversion pending");
            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Conversion>), StatusCodes.Status200OK)]
        public IEnumerable<Conversion> Get() 
            => _mediaConverter.GetAll().Select(Conversion.From);

        [HttpPost("{id:guid}/start")]
        [ProducesResponseType(typeof(Conversion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartConversionAsync([FromRoute] Guid id)
        {
            var media = await _mediaProvider.GetAsync(id);
            if (media == null)
                return BadRequest(Error.NotFound);

            if (await _mediaConverter.StartAsync(media))
                return StatusCode(StatusCodes.Status201Created);

            if (_mediaConverter.TryGetValue(media, out ConversionState? state))
                return Ok(Conversion.From(state!));

            return BadRequest(Error.Describe($"{media} does not meet conversion criteria"));
        }

        [HttpPost("{id:guid}/stop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StopConversionAsync([FromRoute] Guid id)
        {
            var media = await _mediaProvider.GetAsync(id);
            if (media == null)
                return BadRequest(Error.NotFound);
            if (!_mediaConverter.Stop(media))
                return BadRequest(Error.Describe($"{media} has no pending conversion"));

            return Ok();
        }
    }
}
