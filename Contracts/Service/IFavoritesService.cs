using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IFavoritesService
    {
        Task<int> AddFavoriteAsync(Favorites favorite);
        Task<IEnumerable<Favorites>> GetUserFavoritesAsync(int userId);
        Task<bool> RemoveFavoriteAsync(int favoriteId);
        Task<bool> ToggleFavoriteAsync(Favorites toogleFavorite);
    }
}
