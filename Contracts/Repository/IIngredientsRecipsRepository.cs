using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IIngredientsRecipsRepository : IRepository<IngredientsRecips>
    {
        Task<List<IngredientsRecips>> GetByRecipesIdAsync(int recipeId);
        Task<bool> IsIngredientUsedInRecipeAsync(int recipeId, int ingredientId);
        Task<bool> IsIngredientUsedInAnyRecipeAsync(int ingredientId);
    }
}
