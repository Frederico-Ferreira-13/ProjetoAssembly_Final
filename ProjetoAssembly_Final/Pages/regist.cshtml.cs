using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        public string Name { get; set; } = string.Empty;
        [BindProperty]
        public string UserName { get; set; } = string.Empty;
        [BindProperty]
        public string Email { get; set; } = string.Empty;
        [BindProperty]
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
                return RedirectToPage("/Login");
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Erro ao criar conta. Tente novamente.");
            return Page();
        }
    }
}
