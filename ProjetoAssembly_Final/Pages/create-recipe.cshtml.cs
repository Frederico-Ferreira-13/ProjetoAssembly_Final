using Contracts.Repository;
using Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Repo.Repository;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize(Roles = "1,2")]
    public class create_recipeModel : PageModel
    {        
        private readonly IRecipesRepository _recipesRepository;
        private readonly IIngredientsRepository _ingredientsRepository;
        private readonly IIngredientsRecipsRepository _ingredientsRecipsRepository;

        public create_recipeModel(IRecipesRepository recipesRepository, IIngredientsRepository ingredientsRepository,
            IIngredientsRecipsRepository ingredientsRecipsRepository)
        {
            _recipesRepository = recipesRepository;
            _ingredientsRepository = ingredientsRepository;
            _ingredientsRecipsRepository = ingredientsRecipsRepository;
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

        public async Task<IActionResult> OnPostAsync(decimal[] quantityValue, string[] unit, string[] ingredientName)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToPage("/Login");
                }                              

                int userId = int.Parse(userIdClaim);

                bool isApproved = (User.FindFirst(ClaimTypes.Role)?.Value == "2");

                var newRecipe = new Recipes(
                    userId: userId,
                    categoriesId: CategoriaSelecionada,
                    difficultyId: DificuldadeSelecionada,
                    title: Titulo,
                    instructions: Descricao,
                    prepTimeMinutes: PrepTime,
                    cookTimeMinutes: CookTime,
                    servings: Doses,
                    isApproved: isApproved
                );

                await _recipesRepository.CreateAddAsync(newRecipe);

                for (int i = 0; i < ingredientName.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(ingredientName[i]))
                    {
                        var ingredientBase = new Ingredients(ingredientName[i].Trim(), 1);
                        await _ingredientsRepository.CreateAddAsync(ingredientBase);

                        var relection = new IngredientsRecips(
                            newRecipe.RecipesId,
                            ingredientBase.IngredientsId,
                            quantityValue[i],
                            unit[i]
                        );

                        await _ingredientsRecipsRepository.CreateAddAsync(relection);
                    }
                }

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Erro: {ex.Message}");
                return Page();
            }
        }
    }
}
