using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IIngredientsRepository : IRepository<Ingredients>
    {
        Task<List<Ingredients>> Search(string searchIngredient);
        Task<Ingredients?> GetByNameAsync(string ingredientsName);
        Task<bool> IsIngredientUnique(string ingredientUnique, int? excludeId = null);
    }
}
