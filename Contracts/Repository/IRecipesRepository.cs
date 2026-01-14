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
    }
}
