using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoAssembly_Final.Pages.Base;
using Repo.Repository;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize]
    public class create_recipeModel : BaseRecipesPageModel
    {        
        private readonly IIngredientsService _ingredientsService;
        private readonly IUsersService _usersService;
        private readonly ICloudService _cloudService;
        private readonly ILogger<create_recipeModel> _logger;

        public create_recipeModel(IRecipesService recipesService, IIngredientsService ingredientsService, 
            IUsersService usersService, ICloudService cloudService, ILogger<create_recipeModel> logger, 
            ITokenService tokenService) : base(recipesService, tokenService)
        {
            _ingredientsService = ingredientsService;
            _usersService = usersService;
            _cloudService = cloudService;
            _logger = logger;
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

        [BindProperty]
        public IFormFile? RecipeImage { get; set; }

        public string? ImagePreviewUrl { get; set; }

        [BindProperty]
        public List<string> QuantityValue { get; set; } = new();

        [BindProperty]
        public List<string> IngredientName { get; set; } = new();

        [BindProperty]
        public List<string> Unit { get; set; } = new();

        [BindProperty]
        public List<string> ingredientDetail { get; set; } = new();

        public List<string> UnitOptions => RecipeConstants.UnitOptions;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (!ModelState.IsValid)
            {
                await PreencherPreviewImagem();
                return Page();
            }

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;                

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {                   
                    return RedirectToPage("/Login");
                }

                var userResult = await _usersService.GetUserByIdAsync(userId);
                if(!userResult.IsSuccessful || !userResult.Value.IsApproved)
                {                   
                    ModelState.AddModelError(string.Empty, "A sua conta ainda não foi aprovada.");
                    return Page();
                }

                string? image = null;

                if(RecipeImage != null && RecipeImage.Length > 0)
                {
                    image = await _cloudService.UploadImageAsync(RecipeImage);                    
                }

                var newRecipe = new Recipes(
                   userId: userId,
                   categoriesId: SelectedCategory,
                   difficultyId: SelectedDifficulty,
                   title: Title.Trim(),
                   instructions: Description.Trim(),
                   prepTimeMinutes: PrepTime,
                   cookTimeMinutes: CookTime,
                   servings: Servings.Trim(),
                   imageUrl: image
                );

                var recipeResult = await _recipesService.CreateRecipeAsync(newRecipe);

                if(!recipeResult.IsSuccessful)
                {                    
                    ModelState.AddModelError(string.Empty, recipeResult.Message ?? "Erro ao criar receita.");
                    return Page();
                }

                int novoIdGerado = recipeResult.Value.RecipesId;

                var quantitiesParsed = QuantityValue.Select(q =>
                {
                    if (string.IsNullOrWhiteSpace(q))
                    {
                        return 0m;
                    }

                    string normalized = q.Replace(",", ".");
                    return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal result)
                        ? result
                        : 0m;
                }).ToList();

                var ingredientsResult = await _ingredientsService.UpdateRecipeIngredientsAsync(
                    novoIdGerado,
                    quantitiesParsed.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList()!,
                    Unit,
                    IngredientName,
                    ingredientDetail
                );


                if (!ingredientsResult.IsSuccessful)
                {
                    await PreencherPreviewImagem();
                    ModelState.AddModelError(string.Empty, "Receita criada, mas erro nos ingredientes: " + ingredientsResult.Message);
                    return Page();
                }               

                TempData["SuccessMessage"] = "Receita criada com sucesso!";
                return Page();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal");
                await PreencherPreviewImagem();
                ModelState.AddModelError(string.Empty, "Erro: " + ex.Message);
                return Page();
            }
        }

        private async Task PreencherPreviewImagem()
        {
            if (RecipeImage != null && RecipeImage.Length > 0)
            {
                using var ms = new MemoryStream();
                await RecipeImage.CopyToAsync(ms);
                ImagePreviewUrl = $"data:{RecipeImage.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
            }
        }
    }
}