using Cast.Provider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cast.App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IProviderService _providerService;

        public IndexModel(ILogger<IndexModel> logger, IProviderService providerService)
        {
            _logger = logger;
            _providerService = providerService;
        }

        public IActionResult OnGet()
        {
            if (!_providerService.IsCached)
                return Page();

            return RedirectToPage("Library");
        }
    }
}