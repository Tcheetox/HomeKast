using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cast.SharedModels.User;
using static Cast.SharedModels.User.Settings;

namespace Cast.App.Pages
{
    public class Settings : IValidatableObject
    {
        public string? StaticFilesDirectory { get; set; }
        public string? LibraryDirectories { get; set; }
        public string? SubtitlesPreferences { get; set; }
        public string? LanguagePreferences { get; set; }

        public IEnumerable<string> Directories 
            => !string.IsNullOrWhiteSpace(LibraryDirectories)
            ? LibraryDirectories.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            : Enumerable.Empty<string>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Directories.Any(d => !Directory.Exists(d)))
                yield return new ValidationResult($"One or more directory is invalid", new[] { nameof(LibraryDirectories) });
            if (string.IsNullOrEmpty(StaticFilesDirectory))
                yield return new ValidationResult($"Cache directory is missing", new[] { nameof(StaticFilesDirectory) });
            else if (!Directory.Exists(StaticFilesDirectory))
                yield return new ValidationResult($"Cache directory is invalid", new[] { nameof(StaticFilesDirectory) });
        }
    }

    public class SettingsModel : PageModel
    {
        public string ApplicationUrl => $"http://localhost:{_userProfile.Application.Port}";

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
                LanguagePreferences = string.Join(';', _userProfile.Preferences.Select(p => p.Language) ?? Enumerable.Empty<string>()),
                SubtitlesPreferences = string.Join(';', _userProfile.Preferences.Select(p => p.Subtitles) ?? Enumerable.Empty<string>()),
                LibraryDirectories = string.Join(Environment.NewLine, _userProfile.Library?.Directories ?? Enumerable.Empty<string>()),
            };
        }

        public IActionResult OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (_userProfile.TryUpdate(
                Settings!.StaticFilesDirectory,
                Settings!.SubtitlesPreferences,
                Settings!.LanguagePreferences,
                Settings!.Directories.ToList()))
                return new CreatedResult(nameof(Settings), null);

            return new OkResult();
        }
    }
}
