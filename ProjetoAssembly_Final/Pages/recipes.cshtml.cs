using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class recipesModel : PageModel
    {
        private readonly IRecipesService _recipesService;
        private readonly ITokenService _tokenService;

        public recipesModel(IRecipesService recipesService, ITokenService tokenService)
        {
            _recipesService = recipesService;
            _tokenService = tokenService;
        }

        public IEnumerable<Recipes> ListRecipes { get; set; } = new List<Recipes>();

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int P { get; set; } = 1; // Página atual (default = 1)

        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {
            int pageSize = 9;
            if (P < 1) P = 1;

            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            int? currentUserId = userIdResult.IsSuccessful ? userIdResult.Value : null;

            var result = await _recipesService.SearchRecipesAsync(Search, CategoryId, P, pageSize, currentUserId);

            if (result.IsSuccessful && result.Value.Items != null)
            {
                // Os itens já vêm com .IsFavorite preenchido pelo SQL!
                ListRecipes = result.Value.Items;

                // O TotalCount vem do COUNT(*) OVER() do SQL
                TotalPages = (int)Math.Ceiling(result.Value.TotalCount / (double)pageSize);
            }
            else
            {
                ListRecipes = new List<Recipes>();
                TotalPages = 0;
            }
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
                else
                {                   
                    Console.WriteLine($"Toggle failed for recipeId {recipeId}: {result.Error}");
                    return BadRequest(result.Error); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in toggle: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
