using Kast.Api.Problems;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Kast.Api.Extensions
{
    internal static class ProblemDetailsContextExtension
    {
        private readonly static Lazy<PropertyInfo[]> _lookupProperties
            = new(() => { 
                var basePropertyNames = typeof(ProblemDetails).GetProperties().Select(p => p.Name).ToHashSet();
                return typeof(IProblemDetails).GetProperties().Where(p => !basePropertyNames.Contains(p.Name)).ToArray();
            });

        public static void Extend(this ProblemDetailsContext context)
        {
            if (context.ProblemDetails == null)
                return;

            context.ProblemDetails.Extensions.Clear();
            context.ProblemDetails.Type = null;
            
            foreach (var prop in _lookupProperties.Value)
            {
                var lookup = prop.Name.LowerFirstLetter();
                if (context.HttpContext.Items != null)
                {
                    context.HttpContext.Items.TryGetValue(lookup, out var item);
                    context.ProblemDetails.Extensions[lookup] = item;
                }
            }
        }
    }
}
