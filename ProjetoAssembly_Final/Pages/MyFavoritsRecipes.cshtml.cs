using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Model;
using Contracts.Repository;
using Contracts.Service;

namespace ProjetoAssembly_Final.Pages
{
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

        public IEnumerable<Recipes> MyFavoriteRecipes { get; set; } = Enumerable.Empty<Recipes>();

        public async Task OnGetAsync()
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            var userId = userIdResult.IsSuccessful ? userIdResult.Value : 3;

            var favs = await _unitOfWork.Favorites.GetByUserIdAsync(userId);

            MyFavoriteRecipes = favs
                .Select(f => f.Recipe)
                .Where(r => r != null)
                .Cast<Recipes>()
                .Select(r =>
                {
                    r.IsFavorite = true;
                    return r;
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int recipeId)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Unauthorized();
            }

            try
            {
                var result = await _recipesService.ToggleFavoriteAsync(recipeId, userIdResult.Value);
                if (result.IsSuccessful)
                {
                    return new JsonResult(result.Value);
                }   
                
                return BadRequest(result.Error);                
            }
            catch (Exception ex)
            {                
                return BadRequest(ex.Message);
            }
        }
    }
}
