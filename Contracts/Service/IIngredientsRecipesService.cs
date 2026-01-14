using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IIngredientsRecipesService
    {
        Task<Result<IngredientsRecips>> GetIngredientsRecipsIdAsync(int id);
        Task<Result<IEnumerable<IngredientsRecips>>> GetIngredientsRecipsByRecipeIdAsync(int recipeId);
        Task<Result<IngredientsRecips>> CreateIngredientsRecipesAsync(IngredientsRecips newIngredientsRecipes);
        Task<Result<IEnumerable<IngredientsRecips>>> GetAllIngredientsRecipsAsync();
        Task<Result> UpdateIngredientsRecipesAsync(int id, IngredientsRecips ingredientsRecipesToUpdate);
        Task<Result> DeleteIngredientsRecipsAsync(int id);
    }
}
