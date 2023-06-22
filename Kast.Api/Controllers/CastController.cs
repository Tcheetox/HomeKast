using Microsoft.AspNetCore.Mvc;
using Kast.Api.Models;
using Kast.Api.Problems;
using Kast.Provider.Media;
using Kast.Provider.Cast;

namespace Kast.Api.Controllers
{
    [ApiController]
    [Route("cast")]
    public class CastController : Controller
    {
        private readonly IMediaProvider _mediaProvider;
        private readonly ICastProvider _castProvider;
        private readonly ILogger<CastController> _logger;

        public CastController(ILogger<CastController> logger, IMediaProvider mediaProvider, ICastProvider castProvider) 
        { 
            _logger = logger;
            _mediaProvider = mediaProvider;
            _castProvider = castProvider;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Caster>), StatusCodes.Status200OK)]
        public async Task<IEnumerable<Caster>> GetAllAsync() 
            => (await _castProvider.GetAllAsync()).Select(Caster.From);

        [HttpGet("{receiverId:guid}")]
        [ProducesResponseType(typeof(Caster), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync([FromRoute] Guid receiverId)
        {
            var receiver = (await _castProvider.GetAllAsync())
                .FirstOrDefault(c => c.Id == receiverId);
            if (receiver != null)
                return Ok(Caster.From(receiver));

            return NotFound();
        }

        [HttpPost("{receiverId:guid}/start/{mediaId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartAsync([FromRoute] Guid receiverId, [FromRoute] Guid mediaId)
        {
            var media = await _mediaProvider.GetAsync(mediaId);
            if (media == null)
                return NotFound();
            
            if (media.Status != MediaStatus.Playable)
                return BadRequest();

            if (await _castProvider.TryStart(receiverId, media))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PauseAsync([FromRoute] Guid receiverId)
        {
            if (await _castProvider.TryPause(receiverId))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/play")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PlayAsync([FromRoute] Guid receiverId)
        {
            if (await _castProvider.TryPlay(receiverId))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/stop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StopAsync([FromRoute] Guid receiverId)
        {
            if (await _castProvider.TryStop(receiverId))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/seek/{seconds:double}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SeekAsync([FromRoute] Guid receiverId, [FromRoute] double seconds)
        {
            if (await _castProvider.TrySeek(receiverId, seconds))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/subtitles/{idx:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeSubtitlesAsync([FromRoute] Guid receiverId, [FromRoute] int? idx = null)
        {
            if (await _castProvider.TryChangeSubtitles(receiverId, idx))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/mute")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MuteAsync([FromRoute] Guid receiverId)
        {
            if (await _castProvider.TryToggleMute(receiverId, true))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/unmute")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnmuteAsync([FromRoute] Guid receiverId)
        {
            if (await _castProvider.TryToggleMute(receiverId, false))
                return Ok();

            return BadRequest();
        }

        [HttpPost("{receiverId:guid}/volume/{level:float}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnmuteAsync([FromRoute] Guid receiverId, [FromRoute] float level)
        {
            if (await _castProvider.TrySetVolume(receiverId, level))
                return Ok();

            return BadRequest();
        }
    }
}
