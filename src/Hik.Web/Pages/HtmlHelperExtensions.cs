using HtmlTags;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hik.Web.Pages
{
    public static class HtmlHelperExtensions
    {
        public static HtmlTag ValidationDiv(this IHtmlHelper helper)
        {
            var outerDiv = new HtmlTag("div")
                .Id("validationSummary")
                .AddClass("validation-summary-valid")
                .Data("valmsg-summary", true);

            var ul = new HtmlTag("ul");
            ul.Add("li", li => li.Style("display", "none"));

            outerDiv.Children.Add(ul);

            return outerDiv;
        }
    }
}
