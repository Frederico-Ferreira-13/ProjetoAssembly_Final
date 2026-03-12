using Contracts.Repository;
using Contracts.Service;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecipesRepository _recipesRepository;
        private readonly IIngredientsRepository _ingredientsRepository;
        private readonly IIngredientsRecipsRepository _ingredientsRecipsRepository;
        private readonly IUsersService _usersService;

        public create_recipeModel(IUnitOfWork unitOfWork, IRecipesRepository recipesRepository, IIngredientsRepository ingredientsRepository,
            IIngredientsRecipsRepository ingredientsRecipsRepository, IUsersService usersService)
        {
            _unitOfWork = unitOfWork;
            _recipesRepository = recipesRepository;
            _ingredientsRepository = ingredientsRepository;
            _ingredientsRecipsRepository = ingredientsRecipsRepository;
            _usersService = usersService;
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

        public async Task<IActionResult> OnPostAsync(decimal[] quantityValue, string[] unit, string[] ingredientName, 
            string[] ingredientDetail)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToPage("/Login");
                }

                var userResult = await _usersService.GetUserByIdAsync(userId);
                if(!userResult.IsSuccessful || !userResult.Value.IsApproved)
                {
                    ModelState.AddModelError(string.Empty, "A sua conta ainda não foi aprovada. Contacte o administrador.");
                }

                bool isAdmin = userResult.Value.UsersRoleId == 1;

                var newRecipe = new Recipes(
                    userId: userId,
                    categoriesId: SelectedCategory,
                    difficultyId: SelectedDifficulty,
                    title: Title.Trim(),
                    instructions: Description.Trim(),
                    prepTimeMinutes: PrepTime,
                    cookTimeMinutes: CookTime,
                    servings: Servings.Trim(),
                    imageUrl: null
                );

                if (isAdmin)
                {
                    newRecipe.Approve();
                }

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    await _recipesRepository.CreateAddAsync(newRecipe);

                    if (ingredientName != null && ingredientName.Length > 0)
                    {
                        for (int i = 0; i < ingredientName.Length; i++)
                        {
                            string? baseName = ingredientName[i].Trim();
                            if (!string.IsNullOrWhiteSpace(baseName)) continue;
                            {
                                string detail = (ingredientDetail?.Length > i ? ingredientDetail[i]?.Trim() : null) ?? string.Empty;
                                string ingredientFullName = string.IsNullOrEmpty(detail) ? baseName : $"{baseName} ({detail})";

                                decimal qty = (quantityValue?.Length > i ? quantityValue[i] : 0);
                                string unitValue = (unit?.Length > i ? unit[i]?.Trim() : null) ?? "unidade";

                                if(qty <= 0)
                                {
                                    ModelState.AddModelError($"Ingredients({i}).Quantity", "Quantidade deve ser maior que zero.");
                                    continue;
                                }

                                var existingIngredient = await _ingredientsRepository.GetByNameAsync(ingredientFullName);
                                int ingredientId;

                                if (existingIngredient != null)
                                {
                                    ingredientId = existingIngredient.IngredientsId;
                                }
                                else
                                {
                                    var newIngredient = new Ingredients(
                                        ingredientName: ingredientFullName,
                                        ingredientsTypeId: 1 
                                    );

                                    await _ingredientsRepository.CreateAddAsync(newIngredient);
                                    ingredientId = newIngredient.IngredientsId;
                                }

                                if (await _ingredientsRecipsRepository.IsIngredientUsedInRecipeAsync(newRecipe.RecipesId, ingredientId))
                                {
                                    continue;
                                }

                                var link = new IngredientsRecips(
                                    recipesId: newRecipe.RecipesId,
                                    ingredientsId: ingredientId,
                                    quantityValue: qty,
                                    unit: unitValue
                                );

                                await _ingredientsRecipsRepository.CreateAddAsync(link);
                            }
                        }
                    }

                    await _unitOfWork.CommitAsync();
                    return RedirectToPage("/Index");
                }
                catch (Exception ex)
                {
                    _unitOfWork.Rollback();
                    ModelState.AddModelError(string.Empty, $"Erro ao criar receita: {ex.Message}");
                    return Page();
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Erro: {ex.Message}");
                return Page();
            }
        }
    }
}
