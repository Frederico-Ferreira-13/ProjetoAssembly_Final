using Contracts.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages.Base
{
    public class FavoriteRequest
    {
        public int RecipeId { get; set; }
    }

    public class BaseRecipesPageModel : PageModel
    {
        protected readonly IRecipesService _recipesService;
        protected readonly ITokenService _tokenService;

        public BaseRecipesPageModel(IRecipesService recipesService, ITokenService tokenService)
        {
            _recipesService = recipesService;
            _tokenService = tokenService;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {               
                return RedirectToPage("/Login");
            }

            int? currentUserId = userIdResult.IsSuccessful ? userIdResult.Value : null;

            var recipeResult = await _recipesService.GetRecipeByIdAsync(id, currentUserId);
            if (!recipeResult.IsSuccessful) return NotFound();

            var recipe = recipeResult.Value;
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrador");

            if (!isAdmin && recipe.UserId != userIdResult.Value)
            {
                return Forbid();
            }

            var deleteResult = await _recipesService.DeleteRecipeAsync(id);

            if (deleteResult.IsSuccessful)
            {
                TempData["SuccessMessage"] = "Receita eliminada com sucesso!";

                string referer = Request.Headers["Referer"].ToString();
                if (referer.Contains("/perfil") || referer.Contains("/view-perfil"))
                {
                    return RedirectToPage("/perfil");
                }

                if (referer.Contains("/view_recipes"))
                {
                    return RedirectToPage("/recipes");
                }

                if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
                {
                    return LocalRedirect(referer);
                }

                return RedirectToPage("/Index");
            }

            TempData["ErrorMessage"] = "Erro ao eliminar a receita.";
            return RedirectToPage("/recipes");
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync([FromBody] FavoriteRequest request)
        {
            if (request?.RecipeId <= 0) return BadRequest("ID inválido");

            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful) return Unauthorized();

            try
            {
                var result = await _recipesService.ToggleFavoriteAsync(request.RecipeId, userIdResult.Value);
                if (!result.IsSuccessful) return BadRequest(result.Message);

                var countResult = await _recipesService.GetFavoriteCountAsync(request.RecipeId);

                return new JsonResult(new
                {
                    isFavorite = result.Value,
                    newCount = countResult.IsSuccessful ? countResult.Value : 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO BASE] {ex.Message}");
                return StatusCode(500, "Erro interno ao processar favorito.");
            }
        }
    }
}
