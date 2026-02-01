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

        Task<(IEnumerable<Recipes> Items, int TotalCount)> SearchRecipesAsync(string? search, int? categoryId, int page, int pageSize, int? currentUserId);
    }
}
