using LazyCache;
using Cast.SharedModels;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.RegularExpressions;
using Cast.App.Pages;
using Microsoft.Extensions.Caching.Memory;

namespace Cast.App.TagHelpers.Svg
{
    // Read more: https://codepen.io/sosuke/pen/Pjoqqp
    public class SmartSvgTagHelper : TagHelper
    {
        public string Color { get; set; }
        public string Src { get; set; }
        public string Class { get; set; }
        public string Alt { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }

        private readonly IAppCache _lazyCache;

        public SmartSvgTagHelper(IAppCache lazyCache)
        {
            _lazyCache = lazyCache;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var match = Regex.Match(Color, @"rgb\((?<r>\d{1,3}),(?<g>\d{1,3}),(?<b>\d{1,3})\)");

            SvgColor targetColor;
            if (match.Success)
                targetColor = new SvgColor(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
            else
            {
                var (r, g, b) = Helper.HexToRgb(Color);
                targetColor = new SvgColor(r, g, b);
            }

            var filter = _lazyCache.GetOrAdd($"{nameof(SmartSvgTagHelper)}|{targetColor}", 
                () => new SvgFilter(targetColor).Solve().Filter, 
                new MemoryCacheEntryOptions() { Priority = CacheItemPriority.NeverRemove });
            output.TagName = "img";

            if (!string.IsNullOrWhiteSpace(Class))
                output.Attributes.Add("class", Class);
            if (!string.IsNullOrWhiteSpace(Height))
                output.Attributes.Add("height", $"{Height}px");
            if (!string.IsNullOrWhiteSpace(Width))
                output.Attributes.Add("width", $"{Width}px");
            if (!string.IsNullOrWhiteSpace(Alt))
                output.Attributes.Add("alt", Alt);
            
            output.Attributes.Add("src", Src.StartsWith('~') ? Src[1..] : Src);
            output.Attributes.Add("style", filter);
        }
    }
}
