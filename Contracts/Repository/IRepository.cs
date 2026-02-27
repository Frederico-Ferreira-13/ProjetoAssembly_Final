using Core.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IRepository<TEntity> where TEntity : class, IEntity
    {
        Task CreateAddAsync(TEntity entity);
        Task<TEntity?> ReadByIdAsync(int id, bool? onlyActive = true);
        Task<IEnumerable<TEntity>> ReadAllAsync(bool? onlyActive = true);
        Task UpdateAsync(TEntity entity);
        Task RemoveAsync(TEntity entity);

        Task<int> SaveChangesAsync();
    }
}
