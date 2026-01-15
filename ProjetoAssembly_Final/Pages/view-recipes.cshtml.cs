using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class view_recipesModel : PageModel
    {
        private readonly IRecipesService _recipesService;

        public view_recipesModel(IRecipesService recipesService)
        {
            _recipesService = recipesService;
        }

        public Recipes Recipe { get; private set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if(id <= 0) 
            {
                return RedirectToPage("/Index");
            }

            var result = await _recipesService.GetRecipeByIdAsync(id);

            if (result == null || !result.IsSuccessful || result.Value == null)
            {
                if (id >= 1 && id <= 4)
                {
                    if (id == 1) // Sopa
                    {
                        Recipe = Recipes.Reconstitute(1, 1, 1, 1, "Sopa de Legumes Casseira",
                            "1. Descasque os legumes.\n2. Coza em água e sal.\n3. Triture com um fio de azeite.",
                            10, 25, "4 Pessoas", DateTime.Now, null, true);
                        Recipe.SetImageUrl("sopa.jpg");
                    }
                    else if (id == 2) // Carne
                    {
                        Recipe = Recipes.Reconstitute(2, 1, 2, 2, "Arroz de Pato Tradicional",
                            "1. Coza o pato com enchidos.\n2. Refogue o arroz na gordura do pato.\n3. Leve ao forno para dourar.",
                            20, 50, "6 Pessoas", DateTime.Now, null, true);
                        Recipe.SetImageUrl("arroz-de-pato.jpg");
                    }
                    else if (id == 3) // Peixe
                    {
                        Recipe = Recipes.Reconstitute(3, 1, 3, 2, "Bacalhau à Brás",
                            "1. Refogue a cebola e o alho.\n2. Junte o bacalhau desfiado e a batata palha.\n3. Envolva com ovos batidos.",
                            15, 15, "2 Pessoas", DateTime.Now, null, true);
                        Recipe.SetImageUrl("bacalhau.jpg");
                    }
                    else if (id == 4) // Sobremesa
                    {
                        Recipe = Recipes.Reconstitute(4, 1, 4, 1, "Arroz Doce Cremoso",
                            "1. Coza o arroz em leite com casca de limão.\n2. Adicione açúcar e gemas no final.\n3. Polvilhe com canela.",
                            10, 40, "8 Pessoas", DateTime.Now, null, true);
                        Recipe.SetImageUrl("arroz-doce.jpg");
                    }

                    var ing1 = IngredientsRecips.Reconstitute(1, true, id, 1, 2, "Kg");
                }
                else
                {
                    return NotFound();
                }
                return Page();
            }

            Recipe = result.Value!;
            return Page();
        }
    }
}
