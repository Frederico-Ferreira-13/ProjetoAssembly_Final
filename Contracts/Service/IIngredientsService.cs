using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IIngredientsService
    {
        Task<Result<Ingredients>> GetIngredientByIdAsync(int ingredientId);
        Task<Result<IEnumerable<Ingredients>>> SearchIngredientsAsync(string searchIngredient);
        Task<Result<Ingredients>> CreateIngredientAsync(Ingredients newIngredient);
        Task<Result> UpdateIngredientAsync(Ingredients ingredientToUpdate);
        Task<Result> DeleteIngredientAsync(int ingredientId);
    }
}
