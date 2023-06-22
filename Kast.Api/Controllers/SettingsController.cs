using Kast.Api.Models;
using Kast.Api.Problems;
using Kast.Provider;
using Microsoft.AspNetCore.Mvc;

namespace Kast.Api.Controllers
{
    [ApiController]
    [Route("settings")]
    public class SettingsController : Controller
    {
        private readonly ILogger<SettingsController> _logger;
        private readonly SettingsProvider _settingsProvider;

        public SettingsController(ILogger<SettingsController> logger, SettingsProvider settingsProvider)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
        }

        [HttpGet]
        public Settings Get() => _settingsProvider.Settings;
   
        [HttpPut]
        [ProducesResponseType(typeof(Settings), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IProblemDetails), StatusCodes.Status400BadRequest)]
        public IActionResult Edit(Settings settings)
        {
            if (_settingsProvider.TryUpdate(settings))
            {
                _logger.LogInformation("User settings successfully updated");
                return Ok(_settingsProvider.Settings);
            }

            return BadRequest();
        }
    }
}
