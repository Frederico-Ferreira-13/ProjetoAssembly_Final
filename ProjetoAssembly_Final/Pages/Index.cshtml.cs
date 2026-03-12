using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace ProjetoAssembly_Final.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IRecipesService _recipesService;

        public List<Recipes> RecipeList { get; set; } = new List<Recipes>();

        public IndexModel(ILogger<IndexModel> logger, IRecipesService recipesService)
        {
            _logger = logger;
            _recipesService = recipesService;
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("A carregar a página inicial...");

            RecipeList = new List<Recipes>();

            try
            {
                if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
                {
                    var pendingResult = await _recipesService.GetPendingRecipesAsync();
                    ViewData["PendingCount"] = pendingResult.IsSuccessful && pendingResult.Value != null
                        ? pendingResult.Value.Count()
                        : 0;
                }
                else
                {
                    ViewData["PendingCount"] = 0;
                }                    

                var results = await _recipesService.GetAllRecipesAsync();
                if (results != null && results.IsSuccessful && results.Value != null && results.Value.Any())
                {
                    RecipeList = results.Value
                        .OrderByDescending(r => r.FavoriteCount + (r.AverageRating * 10))
                        .ThenByDescending(r => r.RecipesId)
                        .Take(5)
                        .ToList();
                }
                else
                {
                    _logger.LogWarning("Nenhuma receita encontrada. Mostrando mensagem vazia.");
                    RecipeList = MockRecipes.GetFallbackMockRecipes().Take(4).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar destaques");
                RecipeList = MockRecipes.GetFallbackMockRecipes().Take(4).ToList();
            }            
        }        
    }
}
