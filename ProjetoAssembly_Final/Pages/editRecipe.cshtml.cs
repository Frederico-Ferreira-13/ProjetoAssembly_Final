using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Model;
using Contracts.Service;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Web;

namespace ProjetoAssembly_Final.Pages
{
    public class editRecipeModel : PageModel
    {
        private readonly IRecipesService _recipesService;

        public editRecipeModel(IRecipesService recipesService)
        {
            _recipesService = recipesService;
            Input = null!;
        }

        [BindProperty]
        public Recipes Input { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await _recipesService.GetRecipeByIdAsync(id);

            if(result == null || !result.IsSuccessful || result.Value == null)
            {
                TempData["ErrorMessage"] = "Receita não encontrada.";
                return RedirectToPage("/Index");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrador");

            string ownerId = result.Value.UserId.ToString() ?? string.Empty;
            bool isOwner = !string.IsNullOrEmpty(currentUserId) && ownerId.Equals(currentUserId, StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && !isOwner) 
            {
                TempData["ErrorMessage"] = $"Acesso Negado. User: {currentUserId} | IsAdmin: {isAdmin}";
                return RedirectToPage("/view_recipes", new { id = id });
            } 

            Input = result.Value;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _recipesService.UpdateRecipeAsync(Input);

            if (!result.IsSuccessful)
            {
                ModelState.AddModelError(string.Empty, "Erro ao atualizar a receita.");
                return Page();
            }

            return RedirectToPage("/view_recipes", new { id = Input.RecipesId });
        }
    }
}
