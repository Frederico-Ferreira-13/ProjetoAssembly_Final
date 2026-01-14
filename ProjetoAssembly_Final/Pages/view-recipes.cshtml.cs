using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class view_recipesModel : PageModel
    {
        public Recipes Recipe { get; private set; } = default!;

        public void OnGet()
        {
        }
    }
}
