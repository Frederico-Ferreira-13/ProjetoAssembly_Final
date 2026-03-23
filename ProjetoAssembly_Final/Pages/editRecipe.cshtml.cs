using Contracts.Service;
using Core.Common;
using Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize]
    public class editRecipeModel : PageModel
    {
        private readonly IRecipesService _recipesService;
        private readonly IUsersService _usersService;
        private readonly IIngredientsService _ingredientsService;
        private readonly ICloudService _cloudService;
        private readonly ILogger<editRecipeModel> _logger;

        public editRecipeModel(IRecipesService recipesService, IUsersService usersService, IIngredientsService ingredientsService, ICloudService cloudService,
            ILogger<editRecipeModel> logger)
        {
            _recipesService = recipesService;
            _usersService = usersService;
            _ingredientsService = ingredientsService;
            _cloudService = cloudService;
            _logger = logger;
        }

        [BindProperty]
        public Recipes Input { get; set; } = null!;

        public bool IsReviewMode { get; set; }

        [BindProperty]
        public IFormFile? RecipeImage { get; set; }

        public List<string> UnitOptions => RecipeConstants.UnitOptions;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0) 
            {
                return RedirectToIndex("ID da receita inválido.");
            }

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? currentUserId = int.TryParse(currentUserIdClaim, out int parsedId) ? parsedId : null;

            var result = await _recipesService.GetRecipeByIdAsync(id, currentUserId);
            if (!result.IsSuccessful || result.Value == null) 
            {
                return RedirectToIndex("Receita não encontrada.");
            }                

            var recipe = result.Value;

            var ingredients = await _ingredientsService.GetByRecipesIdWithNamesAsync(id);
            if (ingredients.IsSuccessful && ingredients.Value != null)
            {
                recipe.LoadIngredients(ingredients.Value);
            }

            var authCheck = await CheckUserPermissions(recipe.UserId);
            if (authCheck != null) return authCheck;

            Input = recipe;
            IsReviewMode = !recipe.IsActive;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            _logger.LogInformation("Iniciando OnPostAsync para a receita ID: {Id}", id);

            ValidateBusinessRules();
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido para a receita {Id}", id);
                return Page();
            }

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? currentUserId = int.TryParse(currentUserIdClaim, out int parsedId) ? parsedId : null;

            var result = await _recipesService.GetRecipeByIdAsync(id, currentUserId);
            if (!result.IsSuccessful || result.Value == null) 
            {                
                return RedirectToIndex("Receita não encontrada.");
            }

            var recipeToUpdate = result.Value;

            var authCheck = await CheckUserPermissions(recipeToUpdate.UserId);
            if (authCheck != null)
            {
                return authCheck;
            }            

            try
            {

                if(RecipeImage != null && RecipeImage.Length > 0)
                {
                    _logger.LogInformation("Imagem detetada: {FileName}. Iniciando upload...", RecipeImage.FileName);
                    string imageUrl = await _cloudService.UploadImageAsync(RecipeImage);

                    _logger.LogInformation("Upload concluído. URL recebido: {Url}", imageUrl);
                    recipeToUpdate.SetImageUrl(imageUrl);
                }
                else
                {
                    _logger.LogInformation("Nenhuma nova imagem foi selecionada.");
                }

                recipeToUpdate.UpdateDetails(
                    Input.Title,
                    Input.Instructions,
                    Input.PrepTimeMinutes,
                    Input.CookTimeMinutes,
                    Input.Servings
                );
                recipeToUpdate.ChangeCategory(Input.CategoriesId);
                recipeToUpdate.ChangeDifficulty(Input.DifficultyId);

                _logger.LogInformation("URL Final antes de gravar na DB: {Url}", recipeToUpdate.ImageUrl);

                if (!string.IsNullOrEmpty(Input.ImageUrl)) 
                {
                    recipeToUpdate.SetImageUrl(Input.ImageUrl);
                }

                _logger.LogInformation("Antes do UpdateRecipeAsync - PrepTimeMinutes = {PrepTime}", recipeToUpdate.PrepTimeMinutes);

                var updateRecipeResult = await _recipesService.UpdateRecipeAsync(recipeToUpdate);

                _logger.LogInformation("UpdateRecipeAsync retornou IsSuccessful = {Success}, Message = {Msg}", updateRecipeResult.IsSuccessful, updateRecipeResult.Message);

                if (!updateRecipeResult.IsSuccessful)
                {
                    ProcessServiceErrors(updateRecipeResult);
                    _logger.LogError("Falha no UpdateRecipeAsync: {Msg}", updateRecipeResult.Message);
                    return Page();
                }

                var quantities = Request.Form["QuantityValue"].ToList();
                var units = Request.Form["Unit"].ToList();
                var names = Request.Form["IngredientName"].ToList();
                var details = Request.Form["ingredientDetail"].ToList();


                var quantitiesParsed = quantities.Select(q =>
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
                        id,
                        quantitiesParsed.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList()!,
                        units!,
                        names!,
                        details!);

                if (!ingredientsResult.IsSuccessful)
                {
                    _logger.LogError("Erro ao atualizar ingredientes: {Message}", ingredientsResult.Message);
                    ModelState.AddModelError(string.Empty, ingredientsResult.Message ?? "Erro ao atualizar ingredientes.");
                    return Page();
                }

                TempData["SuccessMessage"] = "Receita e ingredientes atualizados com sucesso!";
                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar a receita {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao guardar as alterações.");
                return Page();
            }
        }

        private async Task<IActionResult?> CheckUserPermissions(int ownerId)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
                return RedirectToPage("/Login");

            var currentUser = (await _usersService.GetUserByIdAsync(currentUserId)).Value;
            if (currentUser == null) return RedirectToPage("/Login");

            bool isAdmin = currentUser.UsersRoleId == 1;
            bool isOwner = ownerId == currentUserId;

            if (!isAdmin && !isOwner)
            {
                TempData["ErrorMessage"] = "Acesso negado.";
                return RedirectToPage("/Index");
            }

            return null;
        }

        private void ValidateBusinessRules()
        {
            if (Input.Title?.Length < 5)
                ModelState.AddModelError("Input.Title", "O título é demasiado curto.");

            if (Input.PrepTimeMinutes <= 0)
                ModelState.AddModelError("Input.PrepTimeMinutes", "Tempo inválido.");
        }

        private void ProcessServiceErrors(Result updateResult)
        {
            if (updateResult.ValidationErrors?.Any() == true)
            {
                foreach (var error in updateResult.ValidationErrors)
                    foreach (var msg in error.Value)
                        ModelState.AddModelError($"Input.{error.Key}", msg);
            }
            else
            {
                ModelState.AddModelError(string.Empty, updateResult.Message ?? "Erro ao atualizar.");
            }
        }            

        private IActionResult RedirectToIndex(string message)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToPage("/Index");
        }
    }
}
