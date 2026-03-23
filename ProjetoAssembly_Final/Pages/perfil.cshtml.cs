using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoAssembly_Final.Pages.Base;
using Service.Services;

namespace ProjetoAssembly_Final.Pages
{
    public class perfilModel : BaseRecipesPageModel
    {        
        private readonly IUsersService _usersService;       

        public perfilModel(IRecipesService recipesService, IUsersService usersService, ITokenService tokenService) 
            : base(recipesService, tokenService)
        {            
            _usersService = usersService;            
        }

        public Users? CurrentUser { get; set; }
        public int TotalCreated { get; set; } = 0;
        public int TotalFavorites { get; set; } = 0;        
        public IEnumerable<Recipes>? MyRecipes { get; set; }
        public string? ErrorMessage { get; set; }
       
        public async Task<IActionResult> OnGetAsync()
        {
            var userResult = await _usersService.GetCurrentUserAsync();
            if (!userResult.IsSuccessful || userResult.Value == null)
            {
                return RedirectToPage("/Login");
            }

            CurrentUser = userResult.Value;

            if (!CurrentUser.IsApproved || !CurrentUser.IsActive)
            {
                ErrorMessage = "A sua conta ainda não está ativa ou aprovada.";
                return Page();
            }

            var createdResult = await _recipesService.GetTotalRecipesByUserAsync(CurrentUser.UserId);
            if (createdResult.IsSuccessful)
            {
                TotalCreated = createdResult.Value;
            }
            else
            {
                ErrorMessage = createdResult.Message ?? "Erro ao carregar total de receitas criadas.";
            }

            var favResult = await _recipesService.GetTotalFavoritesByUserAsync(CurrentUser.UserId);
            if (favResult.IsSuccessful)
            {
                TotalFavorites = favResult.Value;
            }
            else
            {
                ErrorMessage = favResult.Message ?? "Erro ao carregar total de favoritos.";
            }

            var recipesResult = await _recipesService.GetRecipesByUserIdAsync(CurrentUser.UserId);
            if (recipesResult.IsSuccessful && recipesResult.Value != null)
            {
                MyRecipes = recipesResult.Value
                    .Where(r => r.IsActive && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(6)
                    .ToList();
            }
            else
            {
                ErrorMessage = recipesResult.Message ?? "Erro ao carregar as suas receitas.";
            }

            return Page();
        }        
    }
}
