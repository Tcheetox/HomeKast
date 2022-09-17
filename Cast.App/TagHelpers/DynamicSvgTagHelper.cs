using Microsoft.AspNetCore.Razor.TagHelpers;
using LazyCache;
using System.Text.RegularExpressions;

namespace Cast.App.TagHelpers
{
    public class DynamicSvgTagHelper : TagHelper
    {
        private string FilePath
        {
            get
            {
                var basePath = _environment.WebRootPath.Split('/').ToList();
                basePath.AddRange(Source.Replace("~", string.Empty).Replace("/", @"\").Split(@"\"));
                return Path.Combine(basePath.ToArray());
            }
        }

        public string Source { get; set; }
        public string Class { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }

        private readonly IAppCache _lazyCache;
        private readonly IWebHostEnvironment _environment;

        public DynamicSvgTagHelper(IWebHostEnvironment environment, IAppCache lazyCache)
        {
            _environment = environment;
            _lazyCache = lazyCache;
        }

        private static string CleanXml(string content)
            => Regex.Replace(content, @"(?=<\?xml)(.*)(\?>)", string.Empty) // Clear header
                .Replace(Environment.NewLine, string.Empty)
                .Replace("\t", string.Empty);

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var svg = _lazyCache.GetOrAdd($"{nameof(DynamicSvgTagHelper)}|{Source.GetHashCode()}", () =>
            {
                if (!File.Exists(FilePath))
                    return string.Empty;
                return CleanXml(File.ReadAllText(FilePath));
            });

            output.TagName = "svg";

            if (!string.IsNullOrWhiteSpace(Class))
                output.Attributes.Add("class", Class);
            if (!string.IsNullOrWhiteSpace(Height))
                output.Attributes.Add("height", $"{Height}px");
            if (!string.IsNullOrWhiteSpace(Width))
                output.Attributes.Add("width", $"{Width}px");

            output.Attributes.Add("xmlns", Regex.Match(svg, "(?<=xmlns=\")(.*?)(?=\")").Value);
            output.Attributes.Add("viewBox", Regex.Match(svg, "(?<=viewBox=\")(.*?)(?=\")").Value);
            output.Content.SetHtmlContent(Regex.Match(svg, @"(?<=>)(.*)(?=<\/svg)").Value);
        }
    }
}
