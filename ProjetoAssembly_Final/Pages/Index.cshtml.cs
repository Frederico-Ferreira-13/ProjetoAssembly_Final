using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjectoAssembly_Final.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IRecipesService _recipesService;

        public IList<Recipes> ListaReceitas { get; set; } = new List<Recipes>();

        public IndexModel(ILogger<IndexModel> logger, IRecipesService recipesService)
        {
            _logger = logger;
            _recipesService = recipesService;
        }

        public async Task OnGetAsync()
        {

            _logger.LogInformation("A carregar a página inicial...");

            var results = await _recipesService.GetAllRecipesAsync();
            if (results.IsSuccessful && results.Value != null)
            {
                if (results.IsSuccessful && results.Value != null)
                {
                    // Mapeamento do DTO para o Modelo de Domínio
                    ListaReceitas = results.Value.Select(r => Recipes.Reconstitute(
                        r.RecipesId,
                        r.UserId,
                        r.CategoriesId,
                        r.DifficultyId,
                        r.Title,
                        r.Instructions,
                        r.PrepTimeMinutes,
                        r.CookTimeMinutes,
                        r.Servings,
                        r.CreatedAt,
                        r.LastUpdatedAt,
                        !r.IsDeleted
                    )).ToList();
                }
            }
        }
    }
}
