using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Model;
using Contracts.Service;

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
                Input = Recipes.Reconstitute(id, 1, 1, 1, "Receita Exemplo", "...", 10, 20, "4 pessoas", DateTime.Now, null, true);
                return Page();
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
