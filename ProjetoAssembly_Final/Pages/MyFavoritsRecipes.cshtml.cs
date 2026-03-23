using Contracts.Repository;
using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize]
    public class MyFavoritsRecipesModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IRecipesService _recipesService;

        public MyFavoritsRecipesModel(IUnitOfWork unitOfWork, IRecipesService recipesService, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _recipesService = recipesService;
        }

        public class FavoriteRequest { public int RecipeId { get; set; } }

        public IEnumerable<Recipes> MyFavoriteRecipes { get; set; } = Enumerable.Empty<Recipes>();

        public async Task OnGetAsync()
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful || userIdResult.Value == 0)
            {                
                MyFavoriteRecipes = Enumerable.Empty<Recipes>();
                return;
            }

            var userId = userIdResult.Value;

            var favs = await _unitOfWork.Favorites.GetByUserIdAsync(userId);
            var favoriteRecipes = favs
                 .Select(f => f.Recipe)
                 .Where(r => r != null)
                 .Cast<Recipes>()
                 .ToList();

            favoriteRecipes.ForEach(r => r.IsFavorite = true);
            MyFavoriteRecipes = favoriteRecipes;
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync([FromBody] FavoriteRequest request)
        {
            int recipeId = request?.RecipeId ?? 0;
            if (recipeId <= 0)
            {
                return BadRequest("ID inválido");
            }

            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return Unauthorized();
            }

            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return BadRequest("Não foi possível identificar o utilizador");
            }

            try
            {
                var result = await _recipesService.ToggleFavoriteAsync(recipeId, userIdResult.Value);
                var updatedCount = await _recipesService.GetFavoriteCountAsync(recipeId);

                return new JsonResult(new
                {
                    isFavorite = result.Value,
                    newCount = updatedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
