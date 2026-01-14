using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetCategoryByNameAndAccount(string categoryName, int accountId);
        Task<List<Category>> GetRootCategoriesByAccount(int accountId);
        Task<Category?> GetByIdWithSubCategories(int categoryId, int accountId);
        Task<Category?> ReadByIdAndAccountAsync(int id, int accountId);
    }
}
