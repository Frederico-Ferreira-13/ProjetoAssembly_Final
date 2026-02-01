using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Model;
using Contracts.Service;

namespace ProjetoAssembly_Final.Pages
{
    public class perfilModel : PageModel
    {
        public int TotalCriadas { get; set; }
        public int TotalFavoritos { get; set; }


        private readonly IRecipesService _recipesService;

        public perfilModel(IRecipesService recipesService)
        {
            _recipesService = recipesService;
        }

        public async Task OnGetAsync()
        {
            var userId = 3;

            TotalCriadas = await _recipesService.GetTotalRecipesByUserAsync(userId);
            TotalFavoritos = await _recipesService.GetTotalFavoritesByUserAsync(userId);
        }
    }
}
