using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;

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
        public string? Sort { get; set; }

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
                ListRecipes = result.Value.Items;               
                TotalPages = (int)Math.Ceiling(result.Value.TotalCount / (double)pageSize);
            }
            else
            {
                ListRecipes = MockRecipes.GetFallbackMockRecipes();
                TotalPages = 0;
            }
        }        
    }
}
