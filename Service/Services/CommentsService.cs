using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (comment == null)
            {
                return Result<Comments>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Comentário com ID {id} não encontrado.")
                );
            }

            return Result<Comments>.Success(comment);
        }

        public async Task<Result<List<Comments>>> GetAllCommentsAsync()
        {
            var comments = await _commentsRepository.ReadAllAsync();
            return Result<List<Comments>>.Success(comments.ToList());
        }

        public async Task<Result<List<Comments>>> GetCommentsByRecipeIdAsync(int recipeId)
        {
            var comments = await _commentsRepository.GetCommentsByRecipeIdAsync(recipeId);

            return Result<List<Comments>>.Success(comments.ToList());
        }

        public async Task<Result<Comments>> CreateCommentsAsync(Comments newComment)
        {
            var currentUserIdResult = await _usersService.GetCurrentUserIdAsync();

            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value <= 0)
            {
                return Result<Comments>.Failure(
                    Error.Unauthorized(
                    currentUserIdResult.ErrorCode ?? ErrorCodes.AuthUnauthorized,
                    currentUserIdResult.Message ?? "Utilizador não autenticado ou inativo.")
                );
            }

            int currentUserId = currentUserIdResult.Value;

            if (!await _unitOfWork.Recipes.ExistsByIdAsync(newComment.RecipesId))
            {
                return Result<Comments>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Receita com ID {newComment.RecipesId} não encontrada.",
                    new Dictionary<string, string[]> { { nameof(newComment.RecipesId), new[] { "A receita não existe." } } })
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var commentsToCreate = new Comments(
                    recipesId: newComment.RecipesId,
                    userId: currentUserId,
                    rating: newComment.Rating,
                    commentText: newComment.CommentText!
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

        public async Task<Result> UpdateCommentsAsync(int id, Comments updateComments)
        {
            var currentUserIdResult = await _usersService.GetCurrentUserIdAsync(); // Recebe Result<int>
            var existingComments = await _commentsRepository.ReadByIdAsync(id);

            if (existingComments == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Comentário com ID {id} não encontrado para atualização.")
                );
            }

            if (!currentUserIdResult.IsSuccessful || existingComments.UserId != currentUserIdResult.Value)
            {
                return Result.Failure(
                    Error.Forbidden(
                    currentUserIdResult.IsSuccessful ? ErrorCodes.AuthForbidden : currentUserIdResult.ErrorCode!,
                    currentUserIdResult.IsSuccessful ? "Não tem permissão para editar este comentário." : currentUserIdResult.Message!)
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingComments.UpdateComment(updateComments.CommentText!);

                await _commentsRepository.UpdateAsync(existingComments);
                await _unitOfWork.CommitAsync();

                return Result.Success("Comentário atualizado com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();

                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(Error.Validation(
                    "Dados de entrada inválidos ao atualizar o comentário.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar comentário: {ex.Message}"));
            }
        }

        public async Task<Result> DeleteCommentsAsync(int id)
        {
            var currentUserIdResult = await _usersService.GetCurrentUserIdAsync();
            var existingComments = await _commentsRepository.ReadByIdAsync(id);

            if (existingComments == null)
            {
                return Result.Success($"Comentário com ID {id} não encontrado, assumindo que foi eliminado.");
            }

            if (!currentUserIdResult.IsSuccessful || existingComments.UserId != currentUserIdResult.Value)
            {
                return Result.Failure(
                    Error.Forbidden(
                    currentUserIdResult.IsSuccessful ? ErrorCodes.AuthForbidden : currentUserIdResult.ErrorCode!,
                    currentUserIdResult.IsSuccessful ? "Não tem permissão para eliminar este comentário." : currentUserIdResult.Message!)
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingComments.Delete();
                await _commentsRepository.UpdateAsync(existingComments);
                await _unitOfWork.CommitAsync();

                return Result.Success("Comentário eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar comentário: {ex.Message}"));
            }
        }
    }
}
