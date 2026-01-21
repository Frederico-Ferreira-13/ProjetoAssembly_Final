using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Model;
using Contracts.Service;

namespace ProjetoAssembly_Final.Pages
{
    public class perfilModel : PageModel
    {
        private readonly IRecipesService _recipesService;

        public perfilModel(IRecipesService recipesService)
        {
            _recipesService = recipesService;
        }

        public async Task OnGetAsync()
        {

        }
    }
}
