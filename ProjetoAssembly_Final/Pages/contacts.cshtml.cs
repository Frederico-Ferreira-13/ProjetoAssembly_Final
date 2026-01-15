using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ProjetoAssembly_Final.Pages
{
    public class contactsModel : PageModel
    {
        [BindProperty]
        [Display(Name = "Nome")]
        public string? Nome { get; set; }

        [BindProperty]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Por favor, insira um email válido.")]
        public string? Email { get; set; }

        [BindProperty]
        [Display(Name = "Mensagem")]
        public string? Mensagem { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            TempData["SuccessMessage"] = "Obrigado pelo seu contacto! Responderemos o mais breve possível.";

            return RedirectToPage();
        }
    }
}
