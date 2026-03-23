using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IRecipesRepository : IRepository<Recipes>
    {
        Task<List<Recipes>> GetUserIdRecipes(int userId);
        Task<bool> ExistsByIdAsync(int recipeId);

        Task UpdateRecipeAverageRatingAsync(int recipeId);

        Task<(IEnumerable<Recipes> Items, int TotalCount)> SearchRecipesAsync(
            string? search, int? categoryId, int page, int pageSize, int? currentUserId);

        Task<bool> AnyWithDifficultyIdAsync(int difficultyId);
        Task<IEnumerable<Recipes>> GetPendingRecipesAsync();

        Task<IEnumerable<Recipes>> GetRecipesWithFavoritesAsync(int? userId, int? categoryId);

        Task<List<IngredientsRecips>> GetIngredientsByRecipeIdAsync(int recipeId);

        Task UpsertRecipeRatingAsync(int recipeId, int userId, int rating);
    }
}
