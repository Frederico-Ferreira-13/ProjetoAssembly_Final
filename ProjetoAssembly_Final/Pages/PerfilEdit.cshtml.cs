using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace ProjetoAssembly_Final.Pages
{
    public class PerfilEditModel : PageModel
    {
        private readonly IUsersService _usersService;        

        public PerfilEditModel(IUsersService usersService)
        {
            _usersService = usersService;           
        }

        [BindProperty]
        public Users CurrentUser { get; set; }        

        public async Task<IActionResult> OnGetAsync()
        {
            var userResult = await _usersService.GetCurrentUserAsync();

            if (!userResult.IsSuccessful)
            {
                return RedirectToPage("/Login");
            }

            CurrentUser = userResult.Value;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _usersService.UpdateUserProfileAsync(CurrentUser);           
           

            if (result.IsSuccessful)
            {
                TempData["Success"] = "Perfil atualizado com sucesso!";
                return RedirectToPage("/perfil");
            }

            ModelState.AddModelError(string.Empty, "Erro ao atualizar o perfil.");
            return Page();
        }
    }
}
