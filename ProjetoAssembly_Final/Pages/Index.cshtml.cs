using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace ProjectoAssembly_Final.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IRecipesService _recipesService;

        public List<Recipes> ListaReceitas { get; set; } = new List<Recipes>();

        public IndexModel(ILogger<IndexModel> logger, IRecipesService recipesService)
        {
            _logger = logger;
            _recipesService = recipesService;
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("A carregar a página inicial...");
            
            ListaReceitas = new List<Recipes>();

            try
            {
                var results = await _recipesService.GetAllRecipesAsync();
                if (results != null && results.IsSuccessful && results.Value != null)
                {
                    ListaReceitas.AddRange(results.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico ao ligar à BD");
            }
            
            AdicionarReceitasExemplo();
        }

        private void AdicionarReceitasExemplo()
        {
            var sopa = Recipes.Reconstitute(1, 1, 1, 1, "Sopa de Legumes Caseira",
                "1. Descasque os legumes.\n2. Coza em água e sal. \n3. Triture com um fio de azeite.",
                10, 25, "4 Pessoas", DateTime.Now, null, true);
            sopa.SetImageUrl("sopa.jpg");

            var carne = Recipes.Reconstitute(2, 1, 2, 2, "Arroz de Pato Tradicional",
                "1. Coza o pato com enchidos.\n2. Refogue o arroz na gordura do pato.\n3. Leve ao forno para dourar.",
                20, 50, "6 Pessoas", DateTime.Now, null, true);
            carne.SetImageUrl("arroz-de-pato.jpg");

            var peixe = Recipes.Reconstitute(3, 1, 3, 2, "Bacalhau à Brás",
                "1. Refogue a cebola e o alho.\n2. Junte o bacalhau desfiado e a batata palha.\n3. Envolva com ovos batidos.",
                15, 15, "2 Pessoas", DateTime.Now, null, true);
            peixe.SetImageUrl("bacalhau.jpg");

            var doce = Recipes.Reconstitute(4, 1, 4, 1, "Arroz Doce Cremoso",
                "1. Coza o arroz em leite com casca de limão.\n2. Adicione açúcar e gemas no final.\n3. Polvilhe com canela.",
                10, 40, "8 Pessoas", DateTime.Now, null, true);
            doce.SetImageUrl("arroz-doce.jpg");

            ListaReceitas.Add(sopa);
            ListaReceitas.Add(carne);
            ListaReceitas.Add(peixe);
            ListaReceitas.Add(doce);
        }
    }
}
