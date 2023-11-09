using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace Hik.Web.Pages
{
    public class TestModel : PageModel
    {

        [BindProperty]
        public List<string> SelectedValues { get; set; } = new List<string>();
        public List<SelectListItem> YourSelectList { get; set; }
        public TestModel()
        {
        }

        public void OnGet()
        {
            // Initialize the YourSelectList with your options
            YourSelectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Default", Text = "Default",
                                     Disabled = false,Group = null,Selected = false },
                new SelectListItem { Value = "Option1", Text = "Option 1",
                                     Disabled = false,Group = null,Selected = false },
                new SelectListItem { Value = "Option2", Text = "Option 2",
                                     Disabled = false,Group = null,Selected = false },
                new SelectListItem { Value = "Option3", Text = "Option 3",
                                     Disabled = false,Group = null,Selected = false }
            };
            SelectedValues.Add("Default");
        }

        public IActionResult OnPost()
        {
            // breakpoint here to check SelectedValues  binding
            return RedirectToPage("./Test");
        }

        public IActionResult OnPostAddRow(string id, [FromBody] string serializedModel)
        {
            // Deserialize the model
            var model = JsonConvert.DeserializeObject<TestModel>(serializedModel);

            // Add a new item to the SelectedValues list
            model.SelectedValues ??= new List<string>();
            model.SelectedValues.Add("Default");

            // Return the partial view with the updated model
            return Partial("_SelectTable", model);
        }
    }
}