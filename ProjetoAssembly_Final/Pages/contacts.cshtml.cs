using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ProjetoAssembly_Final.Pages
{
    public class contactsModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string? Name { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Por favor, insira um email válido.")]
        public string? Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A mensagem não pode estar vazia.")]
        public string? Message { get; set; }

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
