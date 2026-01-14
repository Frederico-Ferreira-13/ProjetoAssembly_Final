using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IIngredientsTypeService
    {
        Task<Result<IngredientsType>> CreateIngredientsTypeAsync(IngredientsType ingredientsType);
        Task<Result<IngredientsType>> GetIngredientsTypeByIdAsync(int id);
        Task<Result<IEnumerable<IngredientsType>>> GetAllIngredientsTypesAsync();
        Task<Result> UpdateIngredientsTypeAsync(IngredientsType updateIngredientsType);
        Task<Result> DeleteIngredientsTypeAsync(int id);

        Task<Result<IngredientsType>> GetIngredientsTypeByNameAsync(string name);
    }
}
