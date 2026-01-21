using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class PerfilEditModel : PageModel
    {
        [BindProperty]
        public string Nome { get; set; }

        [BindProperty]
        public string Email { get; set; }

        public void OnGet()
        {
            Nome = "Frederico";
            Email = "fredericocrf87@hotmail.com";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            return RedirectToPage("/perfil");
        }
    }
}
