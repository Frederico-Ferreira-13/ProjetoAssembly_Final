using Contracts.Repository;
using Core.Common;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public abstract class SoftDeleteRepository<TEntity> : Repository<TEntity>
        where TEntity : class, IEntity, ISoftDeletable
    {
        protected SoftDeleteRepository(string tableName) : base(tableName) { }

        public override async Task<TEntity?> ReadByIdAsync(int id)
        {
            string sql = $"SELECT * FROM {_tableName} WHERE {PrimaryKeyName} = @Id AND IsActive = 1";
            var parameters = new SqlParameter[] { new SqlParameter("@Id", id) };
            return await ExecuteSingleAsync(sql, parameters);
        }

        public override async Task<IEnumerable<TEntity>> ReadAllAsync()
        {
            string sql = $"SELECT * FROM {_tableName} WHERE IsActive = 1";
            return await ExecuteListAsync(sql, Array.Empty<SqlParameter>());
        }

        public override async Task RemoveAsync(TEntity entity)
        {
            string sql = $"UPDATE {_tableName} SET IsActive = 0, LastUpdatedAt = GETDATE() WHERE {PrimaryKeyName} = @Id";
            SqlParameter paramId = new SqlParameter("@Id", entity.GetId());
            await SQL.ExecuteNonQueryAsync(sql, paramId);
        }

    }
}
