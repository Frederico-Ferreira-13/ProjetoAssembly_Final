using Contracts.Repository;
using Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Repo.Repository;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize]
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
        public string Title { get; set; } = string.Empty;
        [BindProperty]
        public string Description { get; set; } = string.Empty;
        [BindProperty]
        public int SelectedCategory { get; set; }
        [BindProperty]
        public int SelectedDifficulty { get; set; }
        [BindProperty]
        public List<string> Ingredients { get; set; } = new();
        [BindProperty]
        public int PrepTime { get; set; } = 10;
        [BindProperty]
        public int CookTime { get; set; } = 10;
        [BindProperty]
        public string Servings { get; set; } = "2 pessoas";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(decimal[] quantityValue, string[] unit, string[] ingredientName, string[] ingredientDetail)
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
                    categoriesId: SelectedCategory,
                    difficultyId: SelectedDifficulty,
                    title: Title,
                    instructions: Description,
                    prepTimeMinutes: PrepTime,
                    cookTimeMinutes: CookTime,
                    servings: Servings,
                    isApproved: isApproved
                );

                await _recipesRepository.CreateAddAsync(newRecipe);

                if (ingredientName != null)
                {
                    for (int i = 0; i < ingredientName.Length; i++)
                    {
                        string? baseName = ingredientName[i].Trim();

                        if (!string.IsNullOrWhiteSpace(baseName))
                        {
                            string detail = (ingredientDetail != null && i < ingredientDetail.Length)
                                            ? ingredientDetail[i]?.Trim() ?? string.Empty
                                            : string.Empty;

                            string currentUnit = (unit != null && i < unit.Length)
                                                ? unit[i] ?? string.Empty 
                                                : string.Empty;

                            string fullName = string.IsNullOrEmpty(detail) ? baseName : $"{baseName} ({detail})";

                            var ingredientBase = new Ingredients(fullName, 1);
                            await _ingredientsRepository.CreateAddAsync(ingredientBase);

                            var relection = new IngredientsRecips(
                                newRecipe.RecipesId,
                                ingredientBase.IngredientsId,
                                quantityValue.Length > i ? quantityValue[i] : 0,
                                currentUnit
                            );

                            await _ingredientsRecipsRepository.CreateAddAsync(relection);
                        }
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
