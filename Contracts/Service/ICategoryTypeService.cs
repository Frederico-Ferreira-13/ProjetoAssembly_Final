using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface ICategoryTypeService
    {
        Task<Result<CategoryType>> CreateCategoryTypeAsync(CategoryType categoryType);
        Task<Result<CategoryType>> GetCategoryTypeByIdAsync(int id);
        Task<Result<IEnumerable<CategoryType>>> GetAllCategoryTypesAsync();
        Task<Result> UpdateCategoryTypeAsync(CategoryType updateCategoryType);
        Task<Result> DeleteCategoryTypeAsync(int id);

        Task<Result<CategoryType>> GetCategoryTypeByNameAsync(string name);
    }
}
