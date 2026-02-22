using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Model;
using Contracts.Service;

namespace ProjetoAssembly_Final.Pages
{
    public class perfilModel : PageModel
    {
        private readonly IRecipesService _recipesService;
        private readonly IUsersService _usersService;

        public int TotalCreated{ get; set; }
        public int TotalFavorites { get; set; }
        public Users? CurrentUser { get; set; }
        public IEnumerable<Recipes>? MyRecipes { get; set; }
        

        public perfilModel(IRecipesService recipesService, IUsersService usersService)
        {
            _recipesService = recipesService;
            _usersService = usersService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userResult = await _usersService.GetCurrentUserAsync();

            if (!userResult.IsSuccessful)
            {
                return RedirectToPage("/Login");
            }

            CurrentUser = userResult.Value;            

            TotalCreated = await _recipesService.GetTotalRecipesByUserAsync(CurrentUser.UserId);
            TotalFavorites = await _recipesService.GetTotalFavoritesByUserAsync(CurrentUser.UserId);

            var recipesResult = await _recipesService.GetRecipesByUserIdAsync(CurrentUser.UserId);
            MyRecipes = recipesResult.IsSuccessful ? recipesResult.Value : new List<Recipes>();

            return Page();
        }
    }
}
