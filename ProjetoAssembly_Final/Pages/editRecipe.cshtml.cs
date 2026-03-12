using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Common;
using Core.Model;
using Contracts.Service;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjetoAssembly_Final.Pages
{
    [Authorize]
    public class editRecipeModel : PageModel
    {
        private readonly IRecipesService _recipesService;
        private readonly IUsersService _usersService;

        public editRecipeModel(IRecipesService recipesService, IUsersService usersService)
        {
            _recipesService = recipesService;            
            _usersService = usersService;
        }

        [BindProperty]
        public Recipes Input { get; set; } = null!;

        public string? ErrorMessage { get; set; }

        public bool IsReviewMode { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if(id <= 0)
            {
                TempData["ErrorMessage"] = "ID da receita inválido.";
                return RedirectToPage("/Index");
            }            

            Recipes? recipe = null;
            bool isMock = id >= 9991 && id <= 9995;

            if (!isMock)
            {
                var result = await _recipesService.GetRecipeByIdAsync(id);
                if (result?.IsSuccessful == true && result.Value != null)
                {
                    recipe = result.Value;
                }
                else
                {                   
                    TempData["ErrorMessage"] = "Receita não encontrada.";
                    return RedirectToPage("/Index");
                }
            }
            else
            {
                LoadMockRecipe(id);
                recipe = Input;                
            }

            if (recipe == null)
            {                
                TempData["ErrorMessage"] = "Receita não encontrada.";
                return RedirectToPage("/Index");
            }

            Input = recipe;
            IsReviewMode = !recipe.IsActive;

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            {                
                TempData["ErrorMessage"] = "Sessão inválida. Faça login novamente.";
                return RedirectToPage("/Login");
            }

            var currentUserResult = await _usersService.GetUserByIdAsync(currentUserId);
            if (!currentUserResult.IsSuccessful || currentUserResult.Value == null)
            {                
                TempData["ErrorMessage"] = "Sessão inválida.";
                return RedirectToPage("/Login");
            }

            var currentUser = currentUserResult.Value;
            bool isAdmin = currentUser.UsersRoleId == 1;
            bool isOwner = recipe.UserId == currentUserId;           

            if (!isAdmin && !isOwner)
            {                
                TempData["ErrorMessage"] = "Acesso negado. Apenas o criador ou administradores podem editar esta receita.";
                return RedirectToPage("/view_recipes", new { id });
            }
            
            return Page();
        }

        private void LoadMockRecipe(int id)
        {
            var mocks = MockRecipes.GetFallbackMockRecipes();
            var mockRecipe = mocks.FirstOrDefault(r => r.RecipesId == id);
            if (mockRecipe != null)
            {
                Input = mockRecipe;
                Input.IsFavorite = MockRecipes.FavoriteMockIds.Contains(id);                
            }
            else
            {
                Input = null!;                
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.Title) || Input.Title.Length < 5)
            {
                ModelState.AddModelError(nameof(Input.Title), "O título deve ter pelo menos 5 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(Input.Instructions) || Input.Instructions.Length < 20)
            {
                ModelState.AddModelError(nameof(Input.Instructions), "As instruções devem ter pelo menos 20 caracteres.");
            }

            if (Input.PrepTimeMinutes <= 0)
            {
                ModelState.AddModelError(nameof(Input.PrepTimeMinutes), "Tempo de preparação deve ser maior que zero.");
            }

            if (Input.CookTimeMinutes <= 0)
            {
                ModelState.AddModelError(nameof(Input.CookTimeMinutes), "Tempo de cozedura deve ser maior que zero.");
            }

            if (string.IsNullOrWhiteSpace(Input.Servings))
            {
                ModelState.AddModelError(nameof(Input.Servings), "O campo 'Porções' é obrigatório.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return RedirectToPage("/Login");
            }

            var currentUserResult = await _usersService.GetUserByIdAsync(currentUserId);
            if (!currentUserResult.IsSuccessful)
            {
                TempData["ErrorMessage"] = "Sessão inválida.";
                return RedirectToPage("/Login");
            }

            var currentUser = currentUserResult.Value;
            bool isAdmin = currentUser!.UsersRoleId == 1;
            bool isOwner = Input.UserId == currentUserId;

            if (!isAdmin && !isOwner)
            {
                TempData["ErrorMessage"] = "Acesso negado.";
                return RedirectToPage("/view_recipes", new { id = Input.RecipesId });
            }

            var updateResult = await _recipesService.UpdateRecipeAsync(Input);
            if (!updateResult.IsSuccessful)
            {
                // Tratar erros específicos do service
                if (updateResult.ErrorCode == ErrorCodes.AuthForbidden)
                {
                    TempData["ErrorMessage"] = "Não tem permissão para editar esta receita.";
                }
                else if (updateResult.ValidationErrors != null && updateResult.ValidationErrors.Any())
                {
                    foreach (var field in updateResult.ValidationErrors)
                    {
                        foreach (var msg in field.Value)
                        {
                            ModelState.AddModelError(field.Key, msg);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, updateResult.Message ?? "Erro ao atualizar a receita.");
                }

                return Page();
            }

            TempData["SuccessMessage"] = "Receita atualizada com sucesso!";
            return RedirectToPage("/view_recipes", new { id = Input.RecipesId });
        }
    }
}
