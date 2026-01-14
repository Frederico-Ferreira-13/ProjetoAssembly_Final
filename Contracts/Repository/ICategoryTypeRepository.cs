using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface ICategoryTypeRepository : IRepository<CategoryType>
    {
        Task<CategoryType?> GetByNameAsync(string TypeName);
    }
}
