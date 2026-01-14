using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class recipesModel : PageModel
    {
        private readonly IRecipesService _recipesService;

        public recipesModel(IRecipesService recipesService)
        {
            _recipesService = recipesService;
        }

        public IEnumerable<Recipes> ListRecipes { get; set; } = new List<Recipes>();

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        public async Task OnGetAsync()
        {
            var result = await _recipesService.GetAllRecipesAsync();

            if (result.IsSuccessful && result.Value != null)
            {
                if(CategoryId.HasValue && CategoryId.Value > 0)
                {
                    ListRecipes = result.Value
                        .Where(r => r.CategoriesId == CategoryId.Value)
                        .ToList();
                }
                else
                {
                    ListRecipes = result.Value;
                }                    
            }
        }
    }
}
