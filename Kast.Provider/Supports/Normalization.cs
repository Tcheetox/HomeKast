using System.Text.RegularExpressions;

namespace Kast.Provider.Supports
{
    internal static class Normalization
    {
        private readonly static Regex _lonelyCharacter = new(@"[^a-zA-Z\d]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly static Regex _number = new(@"\d", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly static Regex _year = new(@"\d{4}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly static Regex _serie1 = new(@"\bS\d{2}E\d{2,3}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly static Regex _serie2 = new(@"\bep\s?(\.|)\s?\d{1,3}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly static Regex _serie3 = new(@"(?<!\S)\d{2,3}(?!\S)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly static Regex _noise = new(@"\[[^\]]*\]|\([^)]*\)|\{[^}]*\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static (string name, string? episode, string? episodeName, int? year) NameFromPath(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var name = string.Join(" ", fileName
                .Replace("_", " ")
                .Replace(".", " ")
                .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Where(e => !_termExclusions.Contains(e)))
                .Trim();

            string? episode = null;
            string? remainder = null;

            // Look for episode
            if (TryFindEpisode(name, out var refinedName, out var episodeIndex, out var remain))
            {
                name = refinedName;
                episode = episodeIndex;
                remainder = remain;
            }

            int? year = null;
            // Look for year in name
            if (TryFindYear(name, out int parsedYear))
            {
                year = parsedYear;
                var splitted = name.Split(parsedYear.ToString(), StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length > 0)
                    name = splitted[0].Trim();
            }  // Look for year in remainder if any
            else if (TryFindYear(remainder, out parsedYear))
                year = parsedYear;

            // Remove noise
            name = _noise.Replace(name, string.Empty);
            if (!string.IsNullOrWhiteSpace(remainder))
            {
                remainder = _noise.Replace(remainder, string.Empty).Trim();
                var splittedRemainder = remainder.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                remainder = string.Join(" ", splittedRemainder.Where(e => e.Length > 1 || !_lonelyCharacter.IsMatch(e))).Trim();
                remainder = _number.Match(remainder).Success ? null : remainder;
            }

            // Remove lonely chars (single non digit-character must go away)
            var splittedName = name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            name = string.Join(" ", splittedName.Where(c => c.Length > 1 || !_lonelyCharacter.IsMatch(c))).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = fileName;

            return (name, episode, remainder, year);
        }

        private static bool TryFindEpisode(string name, out string refinedName, out string episodeIndex, out string remain)
        {
            refinedName = string.Empty; episodeIndex = string.Empty; remain = string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var episodeMatch = _serie1.Match(name);
            episodeMatch = episodeMatch.Success ? episodeMatch : _serie2.Match(name);
            episodeMatch = episodeMatch.Success ? episodeMatch : _serie3.Match(name);
            if (episodeMatch.Success)
            {
                episodeIndex = episodeMatch.Value;
                var splitted = name.Split(episodeMatch.Value);
                if (splitted.Length == 1)
                    refinedName = name.Replace(episodeMatch.Value, string.Empty).Trim();
                else
                {
                    refinedName = splitted[0].Trim();
                    remain = string.Join(" ", splitted[1..]).Replace(".", string.Empty).Trim();
                }
                return true;
            }

            return false;
        }

        private static bool TryFindYear(string? entry, out int year)
        {
            year = -1;
            if (string.IsNullOrWhiteSpace(entry))
                return false;

            var yearMatch = _year.Match(entry);
            if (yearMatch.Success && int.TryParse(yearMatch.Value, out int parsedYear)
                && parsedYear > 1900 && parsedYear <= (DateTime.Now.Year + 1))
            {
                year = parsedYear;
                return true;
            }

            return false;
        }

        private static readonly HashSet<string> _termExclusions = new(StringComparer.OrdinalIgnoreCase)
        {
            "ep",
            "truefrench",
            "jpr",
            "dc",
            "webrip",
            "jiheff",
            "x264-pophd",
            "4KLight",
            "hdlight",
            "2160p",
            "webdl",
            "web_dl",
            "web-dl",
            "FR EN",
            "en fr",
            "ac3",
            "mhdgz",
            "subfrench",
            "10bit",
            "hdr",
            "amzn",
            "ddp5",
            "dvdrip",
            "HDLight",
            "k-lity",
            "vf2",
            "dsnp",
            "HDR10",
            "dolby vision",
            "dolby",
            "uncut",
            "1080p",
            "1080 p",
            "720p",
            "720 p",
            "10bit",
            "10 bit",
            "vff",
            "multi",
            "web",
            "french",
            "english",
            "bluray",
            "hdtv",
            "hevc",
            "6ch",
            "x265",
            "x265-chk",
            "-shc23",
            "shc23",
            "-dl",
            "-dnt",
            "bdrip",
            "x265",
            "-mgd",
            "mgd",
            "notag",
            "no tag",
            "custom",
            "vfi",
            "mhd",
            "x264",
            "uncensored",
            "vostfr",
            "-dl",
            "-fhd",
            "partie",
            "true",
            "final",
            "7.1",
            "7 1",
            "5.1",
            "5 1",
            " NF ",
            "fansub",
            "264-fraternity",
            "8CH",
            "AvAlon",
            "1-SEL",
            "IMAX",
            "RERiP"
        };
    }
}
