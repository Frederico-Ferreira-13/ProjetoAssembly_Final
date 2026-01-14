using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IRatingRepository : IRepository<Ratings>
    {
        Task<Ratings?> GetRatingByUserIdAndRecipeIdAsync(int userId, int recipesId);
        Task<List<Ratings>> GetRatingsByRecipeIdAsync(int recipeId);
        Task<List<Ratings>> GetAllRatingsByUserIdAsync(int userId);

        Task<double> GetAverageRatingAsync(int recipeId);
        Task<bool> ExistsByUserAndRecipeAsync(int recipeId, int userId);
    }
}
