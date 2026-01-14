using Contracts.Repository;
using Core.Model;
using Contracts.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services
{
    public class FavoritesService : IFavoritesService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FavoritesService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentException(nameof(unitOfWork));
        }

        public async Task<int> AddFavoriteAsync(Favorites favorites)
        {
            var recipe = await _unitOfWork.Recipes.ReadByIdAsync(favorites.RecipesId);
            if (recipe == null)
            {
                throw new Exception("Receita não encontrada.");
            }

            var favorite = new Favorites(favorites.UserId, favorites.RecipesId);
            await _unitOfWork.Favorites.CreateAddAsync(favorite);

            return await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<Favorites>> GetUserFavoritesAsync(int userId)
        {
            var favorites = await _unitOfWork.Favorites.GetByUserIdAsync(userId);

            return favorites.Select(f => new Favorites
            {
                FavoritesId = f.FavoritesId,
                UserId = f.UserId,
                RecipesId = f.RecipesId,
                CreatedAt = f.CreatedAt,
                IsActive = f.IsActive,
                Recipe = f.Recipe
            });
           
        }

        public async Task<bool> RemoveFavoriteAsync(int favoriteId)
        {
            var favorite = await _unitOfWork.Favorites.ReadByIdAsync(favoriteId);
            if (favorite == null)
            {
                return false;
            }
            await _unitOfWork.Favorites.RemoveAsync(favorite);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<bool> ToggleFavoriteAsync(Favorites toggleFavorite)
        {
            var existingFavorites = await _unitOfWork.Favorites.GetByUserIdAsync(toggleFavorite.UserId);
            var favorite = existingFavorites.FirstOrDefault(f => f.RecipesId == toggleFavorite.RecipesId);

            if (favorite != null)
            {
                await _unitOfWork.Favorites.RemoveAsync(favorite);
                await _unitOfWork.CommitAsync();
                return false;
            }
            else
            {
                await AddFavoriteAsync(toggleFavorite);
                return true;
            }
        }
    }
}
