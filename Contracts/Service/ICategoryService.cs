using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface ICategoryService
    {
        Task<Result<Category>> CreateCategoryAsync(Category newCategory);
        Task<Result<Category>> GetCategoryByIdAsync(int categoryId);
        Task<Result<IEnumerable<Category>>> GetCategoriesByUserIdAsync();
        Task<Result<IEnumerable<Category>>> GetUserActiveCategoriesAsync();
        Task<bool> CategoryNameExistsForUserAsync(string categoryName, int userId, int? excludeCategoryId = null);
        Task<Result<Category>> UpdateCategoryAsync(Category updateCategory);
        Task<Result<Category>> DeactivateCategoryAsync(int categoryId);
    }
}
