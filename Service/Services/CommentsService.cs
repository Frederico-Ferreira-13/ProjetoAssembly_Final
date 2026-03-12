using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CommentsService : ICommentsService
    {
        private readonly ICommentsRepository _commentsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsersService _usersService;

        public CommentsService(ICommentsRepository commentsRepository, IUnitOfWork unitOfWork, IUsersService usersService)
        {
            _commentsRepository = commentsRepository ?? throw new ArgumentNullException(nameof(commentsRepository));            
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        }

        public async Task<Result<Comments>> GetCommentsByIdAsync(int id)
        {
            var comment = await _commentsRepository.ReadByIdAsync(id);
            if (comment == null || comment.IsDeleted)
            {
                return Result<Comments>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Comentário com ID {id} não encontrado.")
                );
            }

            return Result<Comments>.Success(comment);
        }       

        public async Task<Result<List<Comments>>> GetCommentsByRecipeIdAsync(int recipeId)
        {
            if(recipeId <= 0)
            {
                return Result<List<Comments>>.Failure(
                    Error.Validation(
                        "ID da receita inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }
             
            var comments = await _commentsRepository.GetCommentsByRecipeIdAsync(recipeId);
            return Result<List<Comments>>.Success(comments.ToList());
        }

        public async Task<Result<Comments>> CreateCommentsAsync(Comments newComment)
        {
            var currentUserIdResult = await _usersService.GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value <= 0)
            {
                return Result<Comments>.Failure(
                    Error.Unauthorized(ErrorCodes.AuthUnauthorized, "Utilizador não autenticado.")
                );
            }

            int currentUserId = currentUserIdResult.Value;

            if(newComment.RecipesId <= 0)
            {
                return Result<Comments>.Failure(
                     Error.Validation(
                         "ID da receita inválido.",
                         new Dictionary<string, string[]> { { nameof(newComment.RecipesId), new[] { "Deve ser maior que zero" } } }
                     )
                 );
            }

            if (!await _unitOfWork.Recipes.ExistsByIdAsync(newComment.RecipesId))
            {
                return Result<Comments>.Failure(
                    Error.NotFound(ErrorCodes.NotFound, $"Receita com ID {newComment.RecipesId} não encontrada.")
                );
            }

            if (string.IsNullOrWhiteSpace(newComment.CommentText))
            {
                return Result<Comments>.Failure(
                    Error.Validation(
                        "O comentário não pode estar vazio.",
                        new Dictionary<string, string[]> { { nameof(newComment.CommentText), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            if (newComment.CommentText.Length > 500)
            {
                return Result<Comments>.Failure(
                    Error.Validation(
                        "O comentário não pode exceder 500 caracteres.",
                        new Dictionary<string, string[]> { { nameof(newComment.CommentText), new[] { "Máximo 500 caracteres" } } }
                    )
                );
            }

            if (newComment.Rating < 1 || newComment.Rating > 5)
            {
                return Result<Comments>.Failure(
                    Error.Validation(
                        "A classificação deve estar entre 1 e 5.",
                        new Dictionary<string, string[]> { { nameof(newComment.Rating), new[] { "Valor entre 1 e 5" } } }
                    )
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var commentsToCreate = new Comments(
                     recipesId: newComment.RecipesId,
                     userId: currentUserId,
                     commentText: newComment.CommentText,
                     rating: newComment.Rating
                 );

                await _commentsRepository.CreateAddAsync(commentsToCreate);
                await _unitOfWork.CommitAsync();

                return Result<Comments>.Success(commentsToCreate);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();

                string fieldName = ex.ParamName ?? "Geral";
                return Result<Comments>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos ao criar o comentário.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }
            catch(Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Comments>.Failure(
                    Error.InternalServer($"Erro inesperado ao criar comentário: {ex.Message}"));
            }
        }

        public async Task<Result> UpdateCommentsAsync(int id, Comments updateComment)
        {
            var currentUserIdResult = await _usersService.GetCurrentUserIdAsync(); // Recebe Result<int>
            if (!currentUserIdResult.IsSuccessful)
            {
                return Result.Failure(currentUserIdResult.Error);
            }

            int currentUserId = currentUserIdResult.Value;

            var existingComment = await _commentsRepository.ReadByIdAsync(id);
            if (existingComment == null || existingComment.IsDeleted)
            {
                return Result.Failure(
                    Error.NotFound(ErrorCodes.NotFound, $"Comentário com ID {id} não encontrado ou eliminado.")
                );
            }

            if (existingComment.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(ErrorCodes.AuthForbidden, "Só o autor pode editar este comentário.")
                );
            }

            if (string.IsNullOrWhiteSpace(updateComment.CommentText))
            {
                return Result.Failure(
                    Error.Validation(
                        "O comentário não pode estar vazio.",
                        new Dictionary<string, string[]> { { nameof(updateComment.CommentText), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            if (updateComment.CommentText.Length > 500)
            {
                return Result.Failure(
                     Error.Validation(
                         "O comentário não pode exceder 500 caracteres.",
                         new Dictionary<string, string[]> { { nameof(updateComment.CommentText), new[] { "Máximo 500 caracteres" } } }
                     )
                 );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingComment.UpdateComment(updateComment.CommentText);               
                await _commentsRepository.UpdateAsync(existingComment);
                await _unitOfWork.CommitAsync();

                return Result.Success("Comentário atualizado com sucesso.");
            }           
            catch (InvalidOperationException ex) // ex.: grace period, já eliminado
            {
                _unitOfWork.Rollback();
                return Result.Failure(Error.BusinessRuleViolation(ErrorCodes.BizInvalidOperation, ex.Message));
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.Validation("Dados inválidos ao atualizar comentário.",
                        new Dictionary<string, string[]> { { ex.ParamName ?? "Geral", new[] { ex.Message } } })
                );
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar comentário: {ex.Message}")
                );
            }
        }

        public async Task<Result> DeleteCommentsAsync(int id)
        {
            var currentUserIdResult = await _usersService.GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful)
            {
                return Result.Failure(currentUserIdResult.Error);
            }

            int currentUserId = currentUserIdResult.Value;

            var existingComment = await _commentsRepository.ReadByIdAsync(id);
            if (existingComment == null || existingComment.IsDeleted)
            {
                return Result.Success("Comentário já eliminado ou não encontrado (idempotente).");
            }            

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingComment.Delete();
                await _commentsRepository.UpdateAsync(existingComment);
                await _unitOfWork.CommitAsync();

                return Result.Success("Comentário eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar comentário: {ex.Message}")
                );
            }
        }
    }
}
