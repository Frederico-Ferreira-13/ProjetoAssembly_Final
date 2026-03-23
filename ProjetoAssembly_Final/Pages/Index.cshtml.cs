using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoAssembly_Final.Pages.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoAssembly_Final.Pages
{
    public class IndexModel : BaseRecipesPageModel
    {
        private readonly ILogger<IndexModel> _logger;        

        public List<Recipes> RecipeList { get; set; } = new List<Recipes>();

        public IndexModel(ILogger<IndexModel> logger, IRecipesService recipesService, ITokenService tokenService) 
            : base(recipesService, tokenService)
        {
            _logger = logger;            
        }       

        public async Task OnGetAsync()
        {
            _logger.LogInformation("A carregar a página inicial...");
            RecipeList = new List<Recipes>();

            try
            {
                var userIdResult = await _tokenService.GetUserIdFromContextAsync();
                int? userId = userIdResult.IsSuccessful ? userIdResult.Value : null;

                if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
                {
                    var pendingResult = await _recipesService.GetPendingRecipesAsync();
                    ViewData["PendingCount"] = pendingResult.IsSuccessful ? pendingResult.Value.Count() : 0;
                }

                var results = await _recipesService.GetRecipesWithFavoritesAsync(userId, null);
                if (results?.IsSuccessful == true && results.Value != null)
                {
                    RecipeList = results.Value
                        .Where(r => r.IsApproved == true)
                        .OrderByDescending(r => r.FavoriteCount + (r.AverageRating * 10))
                        .Take(5)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar destaques");
            }
        }       
    }
}
