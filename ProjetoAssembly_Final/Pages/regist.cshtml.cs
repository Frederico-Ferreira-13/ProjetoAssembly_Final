using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ProjetoAssembly_Final.Pages
{
    public class registModel : PageModel
    {
        private readonly IUsersService _usersService;       

        public registModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [BindProperty]
        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "O nome de utilizador é obrigatório")]
        public string UserName { get; set; } = string.Empty;
        
        [BindProperty]
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A palavra-passe é obrigatória")]
        [MinLength(6, ErrorMessage = "A palavra-passe deve ter pelo menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }            
                
            var result = await _usersService.RegisterUserAsync(UserName, Name, Email, Password);

            if (result.IsSuccessful)
            {
                TempData["Success"] = "Conta criada com sucesso! Já podes fazer login.";
                return RedirectToPage("/login");
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Erro ao criar conta. Tente novamente.");
            return Page();
        }
    }
}
