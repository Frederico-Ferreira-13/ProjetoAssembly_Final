using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IFavoritesService
    {
        Task<Result<int>> GetCurrentUserIdAsync();
        Task<Result> AddFavoriteAsync(Favorites favorite);
        Task<Result<IEnumerable<Favorites>>> GetUserFavoritesAsync(int userId);
        Task<Result> RemoveFavoriteAsync(int favoriteId);
        Task<Result<bool>> ToggleFavoriteAsync(int recipeId);

    }
}
