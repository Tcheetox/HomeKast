using Microsoft.AspNetCore.Mvc;

namespace Kast.Api.Extensions
{
    public static class ControllerExtension
    {
        public static void DescribeProblem(this Controller controller, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;

            if (controller.HttpContext.Items == null)
                return;

            controller.HttpContext.Items["description"] = description;
        }
    }
}
