using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize(Roles = "Admin")]
    public class PendingRecipesModel : PageModel
    {
        private readonly IRecipesService _recipesService;

        public PendingRecipesModel(IRecipesService recipesService)
        {
            _recipesService = recipesService;
        }

        public List<Recipes> PendingRecipes { get; set; } = new();

        public async Task OnGetAsync()
        {
            var result = await _recipesService.GetPendingRecipesAsync();
            if (result.IsSuccessful)
            {
                PendingRecipes = result.Value.ToList();
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var result = await _recipesService.ApproveRecipeAsync(id);
            if (result.IsSuccessful)
            {
                TempData["Success"] = "Receita aprovada com sucesso!";
            }
            else
            {
                TempData["Error"] = result.Message ?? "Erro ao aprovar receita";
            }

            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetCountAsync()
        {
            var result = await _recipesService.GetPendingRecipesAsync();
            int count = 0;

            if(result.IsSuccessful && result.Value != null)
            {
                count = result.Value.Count();
            }

            return new JsonResult(count);
        }
    }
}
