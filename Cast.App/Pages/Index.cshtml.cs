using Cast.Provider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cast.App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IMediaProvider _providerService;

        public IndexModel(ILogger<IndexModel> logger, IMediaProvider providerService)
        {
            _logger = logger;
            _providerService = providerService;
        }
        // TODO: rework ajax loading lib, avoid shitty redirect
        public IActionResult OnGet()
        {
            if (!_providerService.IsCached)
                return Page();

            return RedirectToPage("Library");
        }
    }
}