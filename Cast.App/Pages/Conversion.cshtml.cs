using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cast.Provider.Converter;

namespace Cast.App.Pages
{
    public class ConversionModel : PageModel
    {
        private readonly ILogger<ConversionModel> _logger;
        private readonly IMediaConverter _providerService;

        public ConversionModel(ILogger<ConversionModel> logger, IMediaConverter converter)
        {
            _logger = logger;
            _providerService = converter;
        }

        public IActionResult OnGetConversionState()
        {
            return null;
        }
    }
}
