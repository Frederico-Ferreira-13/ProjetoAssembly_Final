using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoAssembly_Final.Pages.Base;
using System.ClientModel.Primitives;
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
            if(id <= 0) 
            {
                return RedirectToPage("/Index");
            }

            Id = id;
            RecipeId = id;            

            await CarregarDadosDaPagina(id);

            if (Recipe == null)
            {
                return NotFound();
            }                     

            return Page();
        }

        public async Task<IActionResult> OnPostCommentAsync(int? parentCommentId)
        {
            Console.WriteLine($"[DEBUG PAGE] OnPostCommentAsync: RecipeId={RecipeId}, Rating={CommentRating}, Parent={parentCommentId}");

            if (RecipeId <= 0)
            {
                Console.WriteLine("[DEBUG PAGE] Erro: RecipeId inválido.");
                ModelState.AddModelError(string.Empty, "ID da receita inválido.");
                await CarregarDadosDaPagina(RecipeId > 0 ? RecipeId : 1);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(CommentMessage))
            {
                ModelState.AddModelError("CommentMessage", "O comentário não pode estar vazio.");
                await CarregarDadosDaPagina(RecipeId);
                return Page();
            }            

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                Console.WriteLine($"[DEBUG PAGE] Criando objeto Comments. Texto: {CommentMessage?.Substring(0, Math.Min(10, CommentMessage.Length))}...");

                var newComment = new Comments(
                            RecipeId,
                    currentUserId,
                    CommentMessage!,
                    parentCommentId == null ? CommentRating : 1,
                    parentCommentId
                );
                
                var result = await _commentsService.CreateCommentsAsync(newComment);               

                if (result.IsSuccessful)
                {
                    if (parentCommentId == null && CommentRating > 0)
                    {
                        Console.WriteLine($"[DEBUG PAGE] Comentário principal: Atualizando nota para {CommentRating}");
                        await _recipesService.UpdateRecipeRatingAsync(RecipeId, currentUserId, CommentRating);
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG PAGE] Resposta detectada ou rating zero: Não altera a nota global.");
                    }

                    TempData["SuccessMessage"] = "Comentário publicado com sucesso!";
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

            await CarregarDadosDaPagina(RecipeId);
            return Page();
        }

        public async Task<JsonResult> OnPostRateOnly([FromBody] RateRequest data)
        {
            if (data == null)
            {
                Console.WriteLine("[DEBUG PAGE] OnPostRateOnly: Dados recebidos são NULOS.");
                return new JsonResult(new { success = false, message = "Dados inválidos." });
            }

            Console.WriteLine($"[DEBUG PAGE] OnPostRateOnly: Recipe={data.RecipeId}, Rating={data.Rating}");

            if (data == null || data.RecipeId <= 0 || data.Rating < 1 || data.Rating > 5)
                return new JsonResult(new { success = false, message = "Dados inválidos." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                return new JsonResult(new { success = false, message = "Login necessário." });

            try
            {                
                var result = await _recipesService.UpdateRecipeRatingAsync(data.RecipeId, currentUserId, data.Rating);

                if (result.IsSuccessful)
                {
                    Console.WriteLine("[DEBUG PAGE] UpdateRecipeRatingAsync teve sucesso.");
                    return new JsonResult(new { success = true });
                }

                Console.WriteLine($"[DEBUG PAGE] Falha no UpdateRecipeRating: {result.Message}");
                return new JsonResult(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG PAGE ERROR] Exceção no RateOnly: {ex.Message}");
                return new JsonResult(new { success = false, message = "Erro: " + ex.Message });
            }
        }

        public async Task<IActionResult> OnPostEditCommentAsync(int commentId, int recipeId, string editCommentText, int editRating)
        {
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

        private async Task CarregarDadosDaPagina(int id)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            int? currentUserId = userIdResult.IsSuccessful ? userIdResult.Value : null;

            var result = await _recipesService.GetRecipeByIdAsync(id, currentUserId);
            if (result.IsSuccessful && result.Value != null)
            {
                Recipe = result.Value;
                IsReviewMode = !Recipe.IsActive;

                var ingredientsResult = await _recipesService.GetIngredientsByRecipeIdAsync(id);
                if (ingredientsResult.IsSuccessful)
                {
                    Recipe.LoadIngredients(ingredientsResult.Value);
                }
            }

            var commentsResult = await _commentsService.GetCommentsByRecipeIdAsync(id);
            ListComments = commentsResult.IsSuccessful ? (commentsResult.Value ?? new()) : new();
        }        
    }
}
