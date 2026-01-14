using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IIngredientsTypeRepository : IRepository<IngredientsType>
    {
        Task<IngredientsType?> GetByNameAsync(string typeName);
    }
}
