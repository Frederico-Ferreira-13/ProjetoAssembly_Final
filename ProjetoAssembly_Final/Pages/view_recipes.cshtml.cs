using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    public class view_recipesModel : PageModel
    {
        private readonly IRecipesService _recipesService;
        private readonly ICommentsService _commentsService;
        private readonly ITokenService _tokenService;

        private static readonly HashSet<int> _mockFavoriteStatus = new();

        public view_recipesModel(IRecipesService recipesService, ICommentsService commentsService, ITokenService tokenService)
        {
            _recipesService = recipesService;
            _commentsService = commentsService;
            _tokenService = tokenService;
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

            var result = await _recipesService.GetRecipeByIdAsync(id);

            await CarregarDadosDaPagina(id);

            if (Recipe == null)
            {
                return NotFound();
            }                     

            return Page();
        }

        public async Task<IActionResult> OnPostCommentAsync(int? parentCommentId)
        {
            Console.WriteLine("======= DEBUG RESPOSTA (REPLY) =======");
            Console.WriteLine($"RecipeId: {RecipeId}");
            Console.WriteLine($"ParentCommentId recebido: {parentCommentId}");
            Console.WriteLine($"Mensagem: {CommentMessage}");

            if (RecipeId <= 0)
            {
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

            if (parentCommentId == null && (CommentRating < 1 || CommentRating > 5))
            {
                ModelState.AddModelError("CommentRating", "Avaliação deve ser entre 1 e 5 estrelas.");
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
                var newComment = new Comments(
                    RecipeId,
                    currentUserId,
                    CommentMessage,
                    parentCommentId == null ? CommentRating : 1,
                    parentCommentId
                );


                Console.WriteLine($"A criar comentário: RecipeId={newComment.RecipesId}, UserId={newComment.UserId}");

                Console.WriteLine("LOG: Vou chamar o CreateCommentsAsync agora...");

                var result = await _commentsService.CreateCommentsAsync(newComment);

                Console.WriteLine($"LOG: Resposta do Serviço: {result.IsSuccessful}");

                Console.WriteLine($"Resultado: IsSuccessful={result.IsSuccessful}, Message={result.Message}");

                if (result.IsSuccessful)
                {
                    TempData["SuccessMessage"] = "Comentário publicado com sucesso!";
                    return RedirectToPage(new { id = RecipeId });
                }

                ModelState.AddModelError(string.Empty, result.Message ?? "Erro ao publicar comentário.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEÇÃO: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, $"Erro: {ex.Message}");
            }

            await CarregarDadosDaPagina(RecipeId);
            return Page();
        }

        public async Task<IActionResult> OnPostEditCommentAsync(int commentId, int recipeId, string editCommentText, int editRating)
        {
            Console.WriteLine($"[EDIT] CommentId={commentId}, RecipeId={RecipeId}, Text={editCommentText}, Rating={editRating}");

            // Validar inputs
            if (commentId <= 0)
            {
                TempData["ErrorMessage"] = "ID do comentário inválido.";
                return RedirectToPage(new { id = RecipeId });
            }

            if (string.IsNullOrWhiteSpace(editCommentText))
            {
                TempData["ErrorMessage"] = "O comentário não pode estar vazio.";
                return RedirectToPage(new { id = RecipeId });
            }

            if (editCommentText.Length > 500)
            {
                TempData["ErrorMessage"] = "O comentário não pode exceder 500 caracteres.";
                return RedirectToPage(new { id = RecipeId });
            }

            if (editRating < 1 || editRating > 5)
            {
                TempData["ErrorMessage"] = "Avaliação deve ser entre 1 e 5 estrelas.";
                return RedirectToPage(new { id = RecipeId });
            }

            // Verificar utilizador autenticado
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            // Criar objeto para atualização (o serviço valida se é o dono)
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

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int recipeId)
        {
            if(!User.Identity?.IsAuthenticated ?? false)
            {
                return Unauthorized();
            }

            if (recipeId >= 9991 && recipeId <= 9995)
            {
                bool isNowFavorite;
                if (MockRecipes.FavoriteMockIds.Contains(recipeId))
                {
                    MockRecipes.FavoriteMockIds.Remove(recipeId);
                    isNowFavorite = false;
                }
                else
                {
                    MockRecipes.FavoriteMockIds.Remove(recipeId);
                    isNowFavorite = true;
                }

                return new JsonResult(new
                {
                    isFavorite = isNowFavorite,
                    newCount = isNowFavorite ? 124 : 123
                });

            }

            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return BadRequest("Não foi possível identificar o utilziador ");
            }

            try
            {
                var result = await _recipesService.ToggleFavoriteAsync(recipeId, userIdResult.Value);
                if (!result.IsSuccessful)
                {
                    return BadRequest(result.Message ?? "Erro ao alternar favorito.");
                }

                var updatedCount = await _recipesService.GetFavoriteCountAsync(recipeId);

                return new JsonResult(new
                {
                    isFavorite = result.Value,
                    newCount = updatedCount
                });
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Erro ao processar favorito: {ex.Message}");
                return StatusCode(500, "Ocorreu um erro ao processar a solicitação.");
            }
        }

        
 
        private async Task CarregarDadosDaPagina(int id)
        {
            Console.WriteLine($"Carregando dados para receita ID: {id}");

            var result = await _recipesService.GetRecipeByIdAsync(id);
            if(result?.IsSuccessful == true && result.Value != null)
            {
                Recipe = result.Value;
                IsReviewMode = !Recipe.IsActive;
            }
            else if(id >= 9991 && id <= 9995)
            {
                LoadMockRecipe(id);
                IsReviewMode = false;
            }

            var commentsResult = await _commentsService.GetCommentsByRecipeIdAsync(id);
            if(commentsResult.IsSuccessful)
            {
                ListComments = commentsResult.Value ?? new List<Comments>();
                Console.WriteLine($"Comentários carregados: {ListComments.Count}");
            }
            else
            {
                Console.WriteLine($"Erro ao carregar comentários: {commentsResult.Message}");
                ListComments = new List<Comments>();
            }
        }

        private void LoadMockRecipe(int id)
        {
            var mocks = MockRecipes.GetFallbackMockRecipes();
            this.Recipe = mocks.FirstOrDefault(r => r.RecipesId == id);

            if (this.Recipe != null)
            {
                this.Recipe.IsFavorite = MockRecipes.FavoriteMockIds.Contains(id);
                this.IsReviewMode = false;
            }
        }
    }
}
