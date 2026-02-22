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

            if (result?.IsSuccessful == true && result.Value != null)
            {
                Recipe = result.Value;
                IsReviewMode = !Recipe.IsActive;
            }
            else if (id >= 9991 && id <= 9995)
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
                ListComments = commentsResult.Value ?? new List<Comments>();
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
                IsReviewMode = !Recipe.IsActive;
            }
            else if(id >= 9991 && id >= 9995)
            {
                LoadMockRecipe(id);
                IsReviewMode = false;
            }

            var commentsResult = await _commentsService.GetCommentsByRecipeIdAsync(id);
            if(commentsResult.IsSuccessful)
            {
                ListComments = commentsResult.Value ?? new List<Comments>();
            }
        }

        private void LoadMockRecipe(int id)
        {
            switch (id)
            {
                case 9991:
                    Recipe = Recipes.Reconstitute(
                        id: 9991,
                        userId: 1,
                        categoriesId: 1,
                        difficultyId: 1,
                        title: "Arroz Doce Cremoso",
                        instructions: "1. Lave 200g de arroz carolino...\n...",
                        prepTimeMinutes: 10,
                        cookTimeMinutes: 45,
                        servings: "6 pessoas",
                        imageUrl: "arroz-doce.jpg",
                        createdAt: DateTime.Now,
                        lastUpdatedAt: null,
                        isActive: true,
                        favoriteCount: 12,
                        averageRating: 4.8
                    );
                    break;

                case 9992:
                    Recipe = Recipes.Reconstitute(
                        id: 9992,
                        userId: 1,
                        categoriesId: 2,
                        difficultyId: 2,
                        title: "Arroz de Pato",
                        instructions: "1. Coza 1 pato inteiro...\n...",
                        prepTimeMinutes: 30,
                        cookTimeMinutes: 90,
                        servings: "4 pessoas",
                        imageUrl: "arroz-de-pato.jpg",
                        createdAt: DateTime.Now,
                        lastUpdatedAt: null,
                        isActive: true,
                        favoriteCount: 7,
                        averageRating: 3.8
                    );
                    break;

                case 9993:
                    Recipe = Recipes.Reconstitute(
                        id: 9993,
                        userId: 1,
                        categoriesId: 3,
                        difficultyId: 1,
                        title: "Sopa de Legumes Caseira e Reconfortante",
                        instructions: "1. Descasque e corte...\n...",
                        prepTimeMinutes: 15,
                        cookTimeMinutes: 35,
                        servings: "6 pessoas",
                        imageUrl: "sopa.jpg",
                        createdAt: DateTime.Now,
                        lastUpdatedAt: null,
                        isActive: true,
                        favoriteCount: 20,
                        averageRating: 5.0
                    );
                    break;

                case 9994:
                    Recipe = Recipes.Reconstitute(
                        id: 9994,
                        userId: 1,
                        categoriesId: 4,
                        difficultyId: 2,
                        title: "Bacalhau à Brás",
                        instructions: "1. Dessalgue 500g...\n...",
                        prepTimeMinutes: 20,
                        cookTimeMinutes: 25,
                        servings: "4 pessoas",
                        imageUrl: "bacalhau.jpg",
                        createdAt: DateTime.Now,
                        lastUpdatedAt: null,
                        isActive: true,
                        favoriteCount: 15,
                        averageRating: 4.7
                    );
                    break;

                case 9995:
                    Recipe = Recipes.Reconstitute(
                        id: 9995,
                        userId: 1,
                        categoriesId: 5,
                        difficultyId: 3,
                        title: "Bolo de Chocolate Vegan Fofinho",
                        instructions: "1. Misture farinha...\n...",
                        prepTimeMinutes: 15,
                        cookTimeMinutes: 35,
                        servings: "10 fatias",
                        imageUrl: "bolo-de-chocolate-vegan.jpg",
                        createdAt: DateTime.Now,
                        lastUpdatedAt: null,
                        isActive: true,
                        favoriteCount: 15,
                        averageRating: 4.6
                    );
                    break;

                default:
                    Recipe = null;
                    break;
            }
        }
    }
}
