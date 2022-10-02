using System.Net;
using System.Security.Claims;
using System.Text;
using Cast.SharedModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cast.App.Pages
{
    public class Settings
    {
        public string? StaticFilesDirectory { get; set; }
        public string? LibraryDirectories { get; set; }
        public string? SubtitlesPreferences { get; set; }
        public string? LanguagePreferences { get; set; }
    }


    public class SettingsModel : PageModel
    {
        private readonly ILogger<SettingsModel> _logger;
        private readonly UserProfile _userProfile;

        [BindProperty]
        public Settings? Settings { get; set; }

        public SettingsModel(ILogger<SettingsModel> logger, UserProfile userProfile)
        {
            _logger = logger;
            _userProfile = userProfile;

            Settings = new Settings()
            {
                StaticFilesDirectory = _userProfile.Application.StaticFilesDirectory,
                LanguagePreferences = string.Join(';', _userProfile.Preferences?.Language ?? Enumerable.Empty<string>()),
                SubtitlesPreferences = string.Join(';', _userProfile.Preferences?.Subtitles ?? Enumerable.Empty<string>()),
                LibraryDirectories = string.Join(';', _userProfile.Library?.Directories ?? Enumerable.Empty<string>()),
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }



            return RedirectToPage("./Index");
        }
    }
}
