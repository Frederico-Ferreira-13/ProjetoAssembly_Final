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

            Recipes? recipe = null;

            if (result?.IsSuccessful == true && result.Value != null)
            {
                recipe = result.Value;
            }
            else if (id >= 9991 && id <= 9995)
            {
                // Carrega o mock (igual ao view_recipes)
                switch (id)
                {
                    case 9991:
                        recipe = Recipes.Reconstitute(9991, 1, 1, 1, "Arroz Doce Cremoso", "...", 10, 45, "6 pessoas", "arroz-doce.jpg", DateTime.Now, null, true, 12, 4.8);
                        break;
                    case 9992:
                        recipe = Recipes.Reconstitute(9992, 1, 2, 2, "Arroz de Pato", "...", 30, 90, "4 pessoas", "arroz-de-pato.jpg", DateTime.Now, null, true, 7, 3.8);
                        break;
                    case 9993:
                        recipe = Recipes.Reconstitute(9993, 1, 3, 1, "Sopa de Legumes Caseira e Reconfortante", "...", 15, 35, "6 pessoas", "sopa.jpg", DateTime.Now, null, true, 20, 5.0);
                        break;
                    case 9994:
                        recipe = Recipes.Reconstitute(9994, 1, 4, 2, "Bacalhau à Brás", "...", 20, 25, "4 pessoas", "bacalhau.jpg", DateTime.Now, null, true, 15, 4.7);
                        break;
                    case 9995:
                        recipe = Recipes.Reconstitute(9995, 1, 5, 3, "Bolo de Chocolate Vegan Fofinho", "...", 15, 35, "10 fatias", "bolo-de-chocolate-vegan.jpg", DateTime.Now, null, true, 15, 4.6);
                        break;
                }
            }

            if (recipe == null)
            {
                TempData["ErrorMessage"] = "Receita não encontrada.";
                return RedirectToPage("/Index");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrador");
            string ownerId = recipe.UserId?.ToString() ?? string.Empty;
            bool isOwner = !string.IsNullOrEmpty(currentUserId) && ownerId == currentUserId;
            

            if (!isAdmin && !isOwner) 
            {
                TempData["ErrorMessage"] = $"Acesso Negado. User: {currentUserId} | IsAdmin: {isAdmin}";
                return RedirectToPage("/view_recipes", new { id = id });
            } 

            Input = recipe;
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
