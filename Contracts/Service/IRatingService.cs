using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IRatingService
    {
        Task<Result<Ratings>> GetRankingById(int ratingId);
        Task<Result<Ratings>> GetUserRatingForRecipeAsync(int recipeId, int userId);
        Task<Result<List<Ratings>>> GetRatingsByRecipeIdAsync(int recipeId);
        Task<double> GetAverageRatingByRecipeIdAsync(int recipeId);

        Task<Result<Ratings>> CreateRatingAsync(Ratings newRating);
        Task<Result> UpdateRatingAsync(int ratingId, int newRatingValue);
        Task<Result> DeleteRatingAsync(int ratingId);
    }
}
