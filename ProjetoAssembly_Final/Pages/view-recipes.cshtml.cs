using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class view_recipesModel : PageModel
    {
        private readonly IRecipesService _recipesService;
        private readonly ICommentsService _commentsService;

        public view_recipesModel(IRecipesService recipesService, ICommentsService commentsService)
        {
            _recipesService = recipesService;
            _commentsService = commentsService;
        }

        public Recipes Recipe { get; private set; } = default!;
        public List<Comments> ListComments { get; set; } = new();

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public int Rating { get; set; }

        [BindProperty]
        public int RecipeId { get; set; }

        public bool IsReviewMode { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if(id <= 0) 
            {
                return RedirectToPage("/Index");
            }

            RecipeId = id;
            var result = await _recipesService.GetRecipeByIdAsync(id);

            if (result != null && result.IsSuccessful && result.Value != null)
            {
                Recipe = result.Value;
                IsReviewMode = !Recipe.IsActive;
            }
            else if (id >= 1 && id <= 4)
            {                
                LoadMockRecipe(id);
                IsReviewMode = false;
            }

            if (Recipe == null)
            {
                return NotFound();
            }

            var commentsResult = await _commentsService.GetCommentsByRecipeIdAsync(id);
            if(commentsResult.IsSuccessful)
            {
                ListComments = commentsResult.Value;
            }           

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(RecipeId <= 0)
            {
                ModelState.AddModelError("", "Erro ao identificar a receita.");
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid)
            {
                await CarregarDadosDaPagina(RecipeId);
                return Page();                
            }

            var newComment = new Comments(
                recipesId: RecipeId,
                userId: 0,
                rating: Rating,
                commentText: Message
            );

            var result = await _commentsService.CreateCommentsAsync(newComment);

            if (result.IsSuccessful)
            {
                return RedirectToPage(new { id = RecipeId });
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Erro ao publicar comentário.");
            await OnGetAsync(RecipeId);
            return Page();
        }

        private async Task CarregarDadosDaPagina(int id)
        {
            var result = await _recipesService.GetRecipeByIdAsync(id);
            if(result?.IsSuccessful == true && result.Value != null)
            {
                Recipe = result.Value;
            }
            else if(id >= 1 && id >= 4)
            {
                LoadMockRecipe(id);
            }

            var commentsResult = await _commentsService.GetCommentsByRecipeIdAsync(id);
            if(commentsResult.IsSuccessful)
            {
                ListComments = commentsResult.Value;
            }
        }

        private void LoadMockRecipe(int id)
        {
            if (id == 1)
            {
                Recipe = Recipes.Reconstitute(1, 1, 1, 1, "Sopa de Legumes Caseira", "1. Descasque... 2. Coza...", 10, 25, "4 Pessoas", 
                    "sopa.jpg", DateTime.Now, null, true);
                
            }
            else if (id == 2)
            {
                Recipe = Recipes.Reconstitute(2, 1, 2, 2, "Arroz de Pato Tradicional", "1. Coza... 2. Refogue...", 20, 50, "6 Pessoas", 
                    "arroz-de-pato.jpg", DateTime.Now, null, true);                
            }
            else if (id == 3)
            {
                Recipe = Recipes.Reconstitute(3, 1, 3, 2, "Bacalhau à Brás", "1. Refogue... 2. Envolva...",  15, 15, "2 Pessoas",
                    "bacalhau.jpg", DateTime.Now, null, true);                
            }
            else if (id == 4)
            {
                Recipe = Recipes.Reconstitute(4, 1, 4, 1, "Arroz Doce Cremoso", "1. Coza... 2. Polvilhe...", 10, 40, "8 Pessoas",
                    "arroz-doce.jpg", DateTime.Now, null, true);                
            }
        }
    }
}
