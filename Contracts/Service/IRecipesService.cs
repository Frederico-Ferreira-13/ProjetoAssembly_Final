using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IRecipesService
    {
        Task<Result<Recipes>> GetRecipeByIdAsync(int recipeId, int? currentUserId);
        Task<Result<IEnumerable<Recipes>>> GetRecipesByUserIdAsync(int userId);
        Task<Result<IEnumerable<Recipes>>> GetAllRecipesAsync();

        Task<Result<Recipes>> CreateRecipeAsync(Recipes newRecipe);
        Task<Result> UpdateRecipeAsync(Recipes recipeToUpdate);
        Task<Result> DeleteRecipeAsync(int recipeId);
        Task<bool> IsRecipeOwnerAsync(int recipeId);
        Task<bool> ExistsAsync(int recipeId);
        Task<Result<object>> ToggleFavoriteAsync(int recipeId, int userId);
        Task<Result<int>> GetFavoriteCountAsync(int recipeId);
        Task<bool> IsRecipeFavoriteAsync(int recipeId, int userId);

        Task<Result<int>> GetTotalRecipesByUserAsync(int userId);
        Task<Result<int>> GetTotalFavoritesByUserAsync(int userId);
        Task<Result<(IEnumerable<Recipes> Items, int TotalCount)>> SearchRecipesAsync(string? search, int? categoryId, int page, int pageSize, int? currentUserId);

        Task<Result<IEnumerable<Recipes>>> GetPendingRecipesAsync();
        Task<Result> ApproveRecipeAsync(int recipeId);

        Task<Result<IEnumerable<Recipes>>> GetRecipesWithFavoritesAsync(int? userId, int? categoryId);

        Task<Result> UpdateRecipeRatingAsync(int recipeId, int userId, int rating);

        Task<Result<IEnumerable<IngredientsRecips>>> GetIngredientsByRecipeIdAsync(int recipeId);

        Task<Result<IEnumerable<Recipes>>> GetFavoriteRecipesByUserIdAsync(int userId);
    }
}
