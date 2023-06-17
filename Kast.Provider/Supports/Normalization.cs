using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace Kast.Provider.Supports
{
    public static class Normalization
    {
        static Normalization()
        {
            _specificExclusions.AddRange(Enum.GetNames(typeof(VideoSize)));
            _specificExclusions.AddRange(Enum.GetNames(typeof(VideoCodec)));
            _specificExclusions.AddRange(Enum.GetNames(typeof(AudioCodec)));
            _specificExclusions.RemoveAll(e => _specificInclusions.Contains(e));
        }

        private static readonly List<string> _specificExclusions = new()
        {
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
            "_",
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

        private static readonly List<string> _specificInclusions = new()
        {
            "anm"
        };

        private static readonly Regex _cleaningRegex = new(@"[\.\[\]\(\)_]", RegexOptions.Compiled);
        public static (string normalized, string displayed) Names(string path)
        {
            var original = Path.GetFileNameWithoutExtension(path);
            string cleaned = _cleaningRegex.Replace(original, match => match.Value switch
            {
                "." => " ",
                "[" or "]" or "(" or ")" => " ",
                "_" => " ",
                _ => match.Value
            });

            for (int i = 0; i < 2; i++)
            {
                var cleanedArray = cleaned
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Where(e => e.Length > 1 || e.ToLower() == "i".ToLower());
                cleaned = string.Join(' ', cleanedArray);
                foreach (var word in _specificExclusions)
                    cleaned = cleaned.Replace(word, string.Empty, StringComparison.InvariantCultureIgnoreCase);
            }

            cleaned = cleaned.Trim().Capitalize();
            var splittedName = cleaned.Split(' ');
            int idx = -1;
            for (int i = 0; i < splittedName.Length; i++)
                if (splittedName[i].Any(char.IsDigit))
                {
                    idx = i;
                    break;
                }

            string displayName = string.Join(' ', splittedName.Where(e => !e.StartsWith('-')));
            string normalizedName = idx > 0 ? string.Join(' ', splittedName[0..idx]) : displayName;
            return (normalizedName, displayName);
        }
    }
}
