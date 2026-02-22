using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IFavoritesRepository : IRepository<Favorites>
    {
        Task<IEnumerable<Favorites>> GetByUserIdAsync(int userId);
        Task<bool> ExistsAsync(int userId, int recipesId);
        Task<int> GetCountByRecipeIdAsync(int recipeId);
        Task DeleteFavoriteAsync(int userId, int recipeId);
        Task<Favorites?> GetByUserAndRecipeAsync(int userId, int recipesId);
    }
}
