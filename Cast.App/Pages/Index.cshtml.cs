using Cast.Provider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cast.App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IMediaProvider _providerService;

        public IndexModel(IMediaProvider providerService)
        {
            _providerService = providerService;
        }

        public IActionResult OnGet() 
            => !_providerService.IsCached 
            ? Page() 
            : RedirectToPage("Library");
    }
}