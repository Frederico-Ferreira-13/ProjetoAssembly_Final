using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoAssembly_Final.Pages.Base;
using System.ClientModel.Primitives;
using System.Reflection;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    public class RateRequest
    {
        public int RecipeId { get; set; }
        public int Rating { get; set; }
    }

    public class view_recipesModel : BaseRecipesPageModel
    {        
        private readonly ICommentsService _commentsService;       

        private static readonly HashSet<int> _mockFavoriteStatus = new();

        public view_recipesModel(IRecipesService recipesService, ICommentsService commentsService, ITokenService tokenService) : base(recipesService, tokenService)
        {            
            _commentsService = commentsService;            
        }
        
        public Recipes Recipe { get; private set; } = default!;
        public List<Comments> ListComments { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public int RecipeId { get; set; }

        public bool IsReviewMode { get; set; }

        [BindProperty]
        public string CommentMessage { get; set; } = string.Empty;

        [BindProperty]
        public int CommentRating { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Console.WriteLine($"\n[DEBUG GET] A carregar receita ID: {id}");
            if (id <= 0) 
            {
                Console.WriteLine("[DEBUG GET] ID inválido, a redirecionar para Index.");
                return RedirectToPage("/Index");
            }

            Id = id;
            RecipeId = id;
            await LoadPageData(id);

            if (Recipe == null)
            {
                Console.WriteLine($"[DEBUG GET] Receita {id} não encontrada no Service.");
                return NotFound();
            }            

            Console.WriteLine($"[DEBUG GET] Sucesso: {Recipe.Title} carregada com {ListComments.Count} comentários.");
            return Page();
        }

        public async Task<IActionResult> OnPostCommentAsync(int? parentCommentId)
        {
            Console.WriteLine("\n--- [DEBUG POST COMMENT] INÍCIO ---");

            if (parentCommentId == null && int.TryParse(Request.Form["parentCommentId"], out int pId))
            {
                parentCommentId = pId;
            }

            Console.WriteLine($"Form Data -> RecipeId: {RecipeId}, Rating: {CommentRating}, Parent: {parentCommentId}");
            Console.WriteLine($"Message: '{CommentMessage}'");            

            if (string.IsNullOrWhiteSpace(CommentMessage))
            {
                ModelState.AddModelError("CommentMessage", "O comentário não pode estar vazio.");
                await LoadPageData(RecipeId);
                return Page();
            }
                       
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"User Claim ID: {userIdClaim ?? "NULO"}");

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                Console.WriteLine("[DEBUG ERROR] Utilizador não autenticado ou ID inválido.");
                return Unauthorized();
            }

            try
            {
                Console.WriteLine($"[DEBUG PAGE] Criando objeto Comments. Texto: {CommentMessage?.Substring(0, Math.Min(10, CommentMessage.Length))}...");

                var newComment = new Comments(
                    RecipeId,
                    currentUserId,
                    CommentMessage!,
                    0,
                    parentCommentId
                );

                Console.WriteLine("[DEBUG] A chamar _commentsService.CreateCommentsAsync...");
                var result = await _commentsService.CreateCommentsAsync(newComment);               

                if (result.IsSuccessful)
                {
                    Console.WriteLine("[DEBUG] Comentário criado com sucesso.");
                    TempData["SuccessMessage"] = "Comentário publicado!";
                    return RedirectToPage(new { id = RecipeId });
                }

                Console.WriteLine($"[DEBUG PAGE] Falha no Service: {result.Message}");
                ModelState.AddModelError(string.Empty, result.Message ?? "Erro ao publicar comentário.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG PAGE ERROR] Exceção: {ex.Message}");
                ModelState.AddModelError(string.Empty, $"Erro: {ex.Message}");
            }

            await LoadPageData(RecipeId);
            return Page();
        }

        public async Task<JsonResult> OnPostRateOnly([FromBody] RateRequest data)
        {
            Console.WriteLine("\n--- [DEBUG AJAX RATE] Atualização de Estrelas ---");

            if (data == null || data.Rating < 1 || data.Rating > 5)
                return new JsonResult(new { success = false, message = "Avaliação inválida." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
                return new JsonResult(new { success = false, message = "Login necessário." });

            // Atualiza apenas a tabela de Ratings/Receita
            var result = await _recipesService.UpdateRecipeRatingAsync(data.RecipeId, currentUserId, data.Rating);

            Console.WriteLine($"[DEBUG] Nota {data.Rating} guardada para Receita {data.RecipeId}. Sucesso: {result.IsSuccessful}");

            return new JsonResult(new { success = result.IsSuccessful, message = result.Message });
        }

        public async Task<IActionResult> OnPostEditCommentAsync(int commentId, int recipeId, string editCommentText, int editRating)
        {
            Console.WriteLine("\n--- [DEBUG EDIT COMMENT] INÍCIO ---");
            Console.WriteLine($"CommentID: {commentId}, RecipeID: {recipeId}, New Rating: {editRating}");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            var updateData = new Comments(
                recipesId: recipeId,
                userId: currentUserId,
                commentText: editCommentText.Trim(),
                rating: editRating
            );

            var result = await _commentsService.UpdateCommentsAsync(commentId, updateData);
            Console.WriteLine($"[DEBUG] Update bem-sucedido? {result.IsSuccessful}");

            if (result.IsSuccessful)
            {
                TempData["SuccessMessage"] = "Comentário atualizado com sucesso!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message ?? "Erro ao atualizar comentário.";
            }

            return RedirectToPage(new { id = recipeId });
        }

        public async Task<IActionResult> OnPostDeleteCommentAsync(int commentId, int recipeId)
        {
            Console.WriteLine($"[DEBUG PAGE] OnPostDeleteCommentAsync: CommentId={commentId}, RecipeId={recipeId}");
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _commentsService.DeleteCommentsAsync(commentId);

                if (result.IsSuccessful)
                {
                    TempData["SuccessMessage"] = "Comentário eliminado com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message ?? "Erro ao eliminar comentário.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG PAGE ERROR] Erro ao eliminar: {ex.Message}");
                TempData["ErrorMessage"] = "Ocorreu um erro inesperado ao eliminar o comentário.";
            }
            
            return RedirectToPage(new { id = recipeId });
        }

        private async Task LoadPageData(int id)
        {
            Console.WriteLine($"[DEBUG] CarregarDadosDaPagina para ID: {id}");
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            int? currentUserId = userIdResult.IsSuccessful ? userIdResult.Value : null;

            var result = await _recipesService.GetRecipeByIdAsync(id, currentUserId);
            if (result.IsSuccessful && result.Value != null)
            {
                Recipe = result.Value;
                IsReviewMode = !Recipe.IsActive;

                Console.WriteLine($"[DEBUG] Dados Receita -> Título: {Recipe.Title}, Imagem: {Recipe.ImageUrl}");

                var ingredientsResult = await _recipesService.GetIngredientsByRecipeIdAsync(id);
                if (ingredientsResult.IsSuccessful)
                {
                    Recipe.LoadIngredients(ingredientsResult.Value);
                    Console.WriteLine($"[DEBUG] Ingredientes carregados: {ingredientsResult.Value?.Count() ?? 0}");
                }
            }

            var commentsResult = await _commentsService.GetCommentsByRecipeIdAsync(id);
            ListComments = commentsResult.IsSuccessful ? (commentsResult.Value ?? new()) : new();
            Console.WriteLine($"[DEBUG] Comentários carregados: {ListComments.Count}");
        }        
    }
}
