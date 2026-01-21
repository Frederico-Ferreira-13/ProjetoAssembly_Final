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
            var temporaria = new List<Recipes>();

            if (result.IsSuccessful && result.Value != null)
            {
                temporaria.AddRange(result.Value);
            }

            var sopa = Recipes.Reconstitute(1, 1, 1, 1, "Sopa de Legumes Casseira", "Instruções aqui...", 10, 25, "4 Pessoas", DateTime.Now, null, true);
            sopa.SetImageUrl("sopa.jpg");

            var carne = Recipes.Reconstitute(2, 1, 2, 2, "Arroz de Pato Tradicional", "Instruções aqui...", 20, 50, "6 Pessoas", DateTime.Now, null, true);
            carne.SetImageUrl("arroz-de-pato.jpg");

            var peixe = Recipes.Reconstitute(3, 1, 3, 2, "Bacalhau à Brás", "Instruções aqui...", 15, 15, "2 Pessoas", DateTime.Now, null, true);
            peixe.SetImageUrl("bacalhau.jpg");

            var doce = Recipes.Reconstitute(4, 1, 4, 1, "Arroz Doce Cremoso", "Instruções aqui...", 10, 40, "8 Pessoas", DateTime.Now, null, true);
            doce.SetImageUrl("arroz-doce.jpg");

            temporaria.Add(sopa);
            temporaria.Add(carne);
            temporaria.Add(peixe);
            temporaria.Add(doce);

            if (CategoryId.HasValue && CategoryId > 0)
            {
                ListRecipes = temporaria.Where(r => r.CategoriesId == CategoryId.Value).ToList();
            }
            else
            {
                ListRecipes = temporaria;
            }
        }
    }
}
