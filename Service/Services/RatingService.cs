using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services
{
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsersService _usersService;
        private readonly ITokenService _tokenService;
        private readonly IRecipesService _recipesService;

        public RatingService(IRatingRepository ratingRepository, IUnitOfWork unitOfWork,
            IUsersService usersService, ITokenService tokenService, IRecipesService recipesService)
        {
            _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _recipesService = recipesService ?? throw new ArgumentNullException(nameof(recipesService));
        }

        public async Task<Result<Ratings>> GetRankingById(int ratingId)
        {
            var rating = await _ratingRepository.ReadByIdAsync(ratingId);
            if (rating == null)
            {
                return Result<Ratings>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Avaliação com ID {ratingId} não encontrada.")
                );
            }

            return Result<Ratings>.Success(rating);
        }

        public async Task<Result<Ratings>> GetUserRatingForRecipeAsync(int recipeId, int userId)
        {
            if (recipeId <= 0)
            {
                return Result<Ratings>.Failure(
                    Error.Validation(
                        "O ID da receita é inválido.", 
                        new Dictionary<string, string[]> { { nameof(recipeId), new[] { "ID da receita deve ser positivo." } } })
                );
            }

            if (userId <= 0)
            {
                return Result<Ratings>.Failure(
                    Error.Validation(
                        "O ID do Utilizador é inválido.", 
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "ID do utilizador deve ser positivo." } } })
                );
            }

            var rating = await _ratingRepository.GetRatingByUserIdAndRecipeIdAsync(recipeId, userId);
            if (rating == null)
            {
                return Result<Ratings>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Avaliação não encontrada para o Utilizador {userId} na Receita {recipeId}.")
                );
            }
            return Result<Ratings>.Success(rating);
        }

        public async Task<Result<List<Ratings>>> GetRatingsByRecipeIdAsync(int recipeId)
        {
            if (recipeId <= 0)
            {
                return Result<List<Ratings>>.Failure(
                    Error.Validation(
                        "ID da receita inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var ratings = await _ratingRepository.GetRatingsByRecipeIdAsync(recipeId);
            return Result<List<Ratings>>.Success(ratings.ToList());
        }

        public async Task<Result<double>> GetAverageRatingByRecipeIdAsync(int recipeId)
        {
            if (recipeId <= 0)
            {
                return Result<double>.Failure(
                    Error.Validation(
                        "ID da receita inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var average = await _ratingRepository.GetAverageRatingAsync(recipeId);
            return Result<double>.Success(average);
        }

        public async Task<Result<Ratings>> CreateRatingAsync(Ratings newRating)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Result<Ratings>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "O utilizador deve estar autenticado para criar uma avaliação.")
                );
            }

            int currentUserId = userIdResult.Value;

            if (newRating.RecipesId <= 0)
            {
                return Result<Ratings>.Failure(
                    Error.Validation(
                        "ID da receita inválido.",
                        new Dictionary<string, string[]> { { nameof(newRating.RecipesId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var recipeResult = await _recipesService.GetRecipeByIdAsync(newRating.RecipesId);
            if (!recipeResult.IsSuccessful)
            {
                return Result<Ratings>.Failure(recipeResult.Error);
            }

            var recipe = recipeResult.Value;
            if (!recipe.IsActive)
            {
                return Result<Ratings>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Receita com ID {newRating.RecipesId} está inativa."
                    )
                );
            }

            if (await _ratingRepository.ExistsByUserAndRecipeAsync(newRating.RecipesId, currentUserId))
            {
                return Result<Ratings>.Failure(
                    Error.Conflict(
                        ErrorCodes.AlreadyExists,
                        "O utilizador já avaliou esta receita. Use 'UpdateRating' para alterar."
                    )
                );
            }

            if (newRating.RatingValue < 1 || newRating.RatingValue > 5)
            {
                return Result<Ratings>.Failure(
                    Error.Validation(
                        "A classificação deve estar entre 1 e 5.",
                        new Dictionary<string, string[]> { { nameof(newRating.RatingValue), new[] { "Valor entre 1 e 5" } } }
                    )
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var ratingToCreate = new Ratings(
                    recipesId: newRating.RecipesId,
                    userId: currentUserId,
                    ratingValue: newRating.RatingValue
                );

                await _ratingRepository.CreateAddAsync(ratingToCreate);
                await _unitOfWork.CommitAsync();

                return Result<Ratings>.Success(ratingToCreate);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Ratings>.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos ao criar avaliação.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Ratings>.Failure(
                    Error.InternalServer($"Erro ao criar avaliação: {ex.Message}"));
            }
        }

        public async Task<Result> UpdateRatingAsync(int ratingId, int newRatingValue)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Result.Failure(
                    Error.Unauthorized(
                        ErrorCodes.AuthUnauthorized,
                        "O utilizador deve estar autenticado para atualizar uma avaliação.")
                );
            }

            int currentUserId = userIdResult.Value;

            var existingRating = await _ratingRepository.ReadByIdAsync(ratingId);
            if (existingRating == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Avaliação com ID {ratingId} não encontrada para atualização.")
                );
            }

            if (existingRating.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Não tem permissão para atualizar esta avaliação."));
            }

            if (newRatingValue < 1 || newRatingValue > 5)
            {
                return Result.Failure(
                    Error.Validation(
                        "A classificação deve estar entre 1 e 5.",
                        new Dictionary<string, string[]> { { nameof(newRatingValue), new[] { "Valor entre 1 e 5" } } }
                    )
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingRating.UpdateRating(newRatingValue);
                await _ratingRepository.UpdateAsync(existingRating);
                await _unitOfWork.CommitAsync();

                return Result.Success("Avaliação atualizada com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos ao atualizar avaliação.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar avaliação: {ex.Message}"));
            }
        }

        public async Task<Result> DeleteRatingAsync(int ratingId)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Result.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "O utilizador deve estar autenticado para remover uma avaliação.")
                );
            }

            int currentUserId = userIdResult.Value;

            var existingRating = await _ratingRepository.ReadByIdAsync(ratingId);
            if (existingRating == null)
            {
                return Result.Success("Avaliação não encontrada ou já eliminada.");
            }

            if (existingRating.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "O Utilizador não tem permissão para remover esta avaliação.")
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _ratingRepository.RemoveAsync(existingRating);
                await _unitOfWork.CommitAsync();

                return Result.Success("Avaliação removida com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao remover avaliação: {ex.Message}"));
            }
        }
    }
}
