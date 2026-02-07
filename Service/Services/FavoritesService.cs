using Contracts.Repository;
using Core.Model;
using Contracts.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Common;

namespace Service.Services
{
    public class FavoritesService : IFavoritesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsersService _usersService;

        public FavoritesService(IUnitOfWork unitOfWork, IUsersService usersService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentException(nameof(unitOfWork));
            _usersService = usersService ?? throw new ArgumentException(nameof(usersService));
        }

        public async Task<Result<int>> GetCurrentUserIdAsync()
        {
            var userIdResult = await _usersService.GetCurrentUserIdAsync();
            if(!userIdResult.IsSuccessful || userIdResult.Value <= 0)
            {
                return Result<int>.Failure(
                    Error.Unauthorized(
                        userIdResult.ErrorCode ?? ErrorCodes.AuthUnauthorized,
                        userIdResult.Message ?? "Utilizador não autenticado."));
            }

            return Result<int>.Success(userIdResult.Value);
        }

        public async Task<Result> AddFavoriteAsync(Favorites favorites)
        {
            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful)
            {
                return Result.Failure(currentUserIdResult.Error);
            }
            int currentUserId = currentUserIdResult.Value;

            if(favorites.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Não pode adicionar favorito para outro utilizador."));
            }

            var recipe = await _unitOfWork.Recipes.ReadByIdAsync(favorites.RecipesId);
            if(recipe == null || !recipe.IsActive)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Receita com ID {favorites.RecipesId} não encontrada ou inativa."));
            }

            var existing = await  _unitOfWork.Favorites.GetByUserAndRecipeAsync(currentUserId, favorites.RecipesId);
            if(existing != null && existing.IsActive)
            {
                return Result.Success("Receita já está nos favoritos");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var favorite = new Favorites(currentUserId, favorites.RecipesId);
                await _unitOfWork.Favorites.CreateAddAsync(favorite);

                await _unitOfWork.CommitAsync();
                return Result.Success("Favorito adicionado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao adicionar favorito: {ex.Message}"));
            }
        }

        public async Task<Result<IEnumerable<Favorites>>> GetUserFavoritesAsync(int userId)
        {
            var currentUserIdResult = await GetCurrentUserIdAsync();
            if(!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userId)
            {
                return Result<IEnumerable<Favorites>>.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Não pode ver favoritos de outro utilizador."));
            }

            var favorites = await _unitOfWork.Favorites.GetByUserIdAsync(userId);
            return Result<IEnumerable<Favorites>>.Success(favorites);     
        }

        public async Task<Result> RemoveFavoriteAsync(int favoriteId)
        {
            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful)
            {
                return Result.Failure(currentUserIdResult.Error);
            }
            int currentUserId = currentUserIdResult.Value;

            var favorite = await _unitOfWork.Favorites.ReadByIdAsync(favoriteId);
            if (favorite == null)
            {
                return Result.Success("Favorito já removido.");
            }

            if(favorite.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Não pode remover favorito de outro utilizador"));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.Favorites.RemoveAsync(favorite);
                await _unitOfWork.CommitAsync();
                return Result.Success("Favorito removido com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao remover favorito: {ex.Message}"));
            }            
        }

        public async Task<Result<bool>> ToggleFavoriteAsync(int recipeId)
        {
            var currentUserIdResult = await GetCurrentUserIdAsync();
            if(!currentUserIdResult.IsSuccessful)
            {
                return Result<bool>.Failure(currentUserIdResult.Error);
            }
            int currentUserId = currentUserIdResult.Value;


            var recipe = await _unitOfWork.Recipes.ReadByIdAsync(recipeId);
            if(recipe == null || !recipe.IsActive)
            {
                return Result<bool>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Receita com ID {recipe} não encontrado ou inativa"));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var existing = await _unitOfWork.Favorites.GetByUserAndRecipeAsync(currentUserId, recipeId);

                bool isNowFavorite;

                if(existing != null && existing.IsActive)
                {
                    existing.Deactivate();
                    await _unitOfWork.Favorites.UpdateAsync(existing);
                    isNowFavorite = false;
                }
                else
                {
                    var newFavorite = new Favorites(currentUserId, recipeId);
                    await _unitOfWork.Favorites.CreateAddAsync(newFavorite);
                    isNowFavorite = true;
                }

                await _unitOfWork.CommitAsync();

                return Result<bool>.Success(isNowFavorite);
            }
            catch(Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<bool>.Failure(
                    Error.InternalServer($"Erro ao togglear favorito: {ex.Message}"));
            }
        }
    }
}
