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
            var userId = userIdResult.IsSuccessful ? userIdResult.Value : 0;

            var favs = await _unitOfWork.Favorites.GetByUserIdAsync(userId);
            var list = favs.Select(f => f.Recipe).Where(r => r != null).Cast<Recipes>().ToList();
            list.ForEach(r => r.IsFavorite = true);

            // 2. INJETAR OS MOCKS que foram marcados
            var allMocks = MockRecipes.GetFallbackMockRecipes();
            foreach (var mockId in MockRecipes.FavoriteMockIds)
            {
                var mock = allMocks.FirstOrDefault(m => m.RecipesId == mockId);
                if (mock != null)
                {
                    mock.IsFavorite = true;
                    list.Add(mock);
                }
            }

            MyFavoriteRecipes = list;
        }
        
        public async Task<IActionResult> OnPostToggleFavoriteAsync(int recipeId)
        {           
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return Unauthorized();
            }

            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return BadRequest("Não foi possível identificar o utilizador.");
            }

            var userId = userIdResult.Value;

            try
            {
                var result = await _recipesService.ToggleFavoriteAsync(recipeId, userIdResult.Value);
                if (!result.IsSuccessful)
                {
                    return BadRequest(result.Message ?? "Erro ao alternar favorito.");
                }

                var updatedCount = await _recipesService.GetFavoriteCountAsync(recipeId);

                return new JsonResult(new
                {
                    isFavorite = result.Value,
                    newCount = updatedCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Erro interno ao processar favorito: " + ex.Message);
            }
        }
    }
}
