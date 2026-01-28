using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IRecipesService
    {

        Task<Result<Recipes>> GetRecipeByIdAsync(int recipeId);
        Task<Result<IEnumerable<Recipes>>> GetAllRecipesAsync();

        Task<Result<Recipes>> CreateRecipeAsync(Recipes newRecipe);
        Task<Result> UpdateRecipeAsync(Recipes recipeToUpdate);
        Task<Result> DeleteRecipeAsync(int recipeId);
        Task<bool> IsRecipeOwnerAsync(int recipeId);
        Task<bool> ExistsAsync(int recipeId);
        Task<Result<object>> ToggleFavoriteAsync(int recipeId, int userId);
    }
}
