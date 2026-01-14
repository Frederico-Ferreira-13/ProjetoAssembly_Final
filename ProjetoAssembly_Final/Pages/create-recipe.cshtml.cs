using Contracts.Repository;
using Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize(Roles = "1,2")]
    public class create_recipeModel : PageModel
    {        
        private readonly IRecipesRepository _recipesRepository;
        private readonly IIngredientsRepository _ingredientsRepository;

        public create_recipeModel(IRecipesRepository recipesRepository, IIngredientsRepository ingredientsRepository)
        {
            _recipesRepository = recipesRepository;
            _ingredientsRepository = ingredientsRepository;
        }
        [BindProperty]
        public string Titulo { get; set; } = string.Empty;
        [BindProperty]
        public string Descricao { get; set; } = string.Empty;
        [BindProperty]
        public int CategoriaSelecionada { get; set; }
        [BindProperty]
        public int DificuldadeSelecionada { get; set; }
        [BindProperty]
        public List<string> Ingredientes { get; set; } = new();
        [BindProperty]
        public int PrepTime { get; set; } = 10;
        [BindProperty]
        public int CookTime { get; set; } = 10;
        [BindProperty]
        public string Doses { get; set; } = "2 pessoas";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToPage("/Login");
                }

                int userId = int.Parse(userIdClaim);

                bool isApproved = (userRoleClaim == "2");

                var newRecipe = new Recipes(
                    userId: userId,
                    categoriesId: CategoriaSelecionada,
                    difficultyId: DificuldadeSelecionada,
                    title: Titulo,
                    instructions: Descricao,
                    prepTimeMinutes: 10,
                    cookTimeMinutes: 10,
                    servings: "2 pessoas",
                    isApproved: isApproved
                );

                await _recipesRepository.CreateAddAsync(newRecipe);

                foreach (var ingredientsName in Ingredientes)
                {
                    if (!string.IsNullOrWhiteSpace(ingredientsName))
                    {
                        var ingredient = new Ingredients(ingredientsName.Trim(), newRecipe.RecipesId);
                        await _ingredientsRepository.CreateAddAsync(ingredient);
                    }
                }

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Erro de guardar receita: {ex.Message}");
                return Page();
            }
        }
    }
}
