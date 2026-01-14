using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class IngredientsTypeRepository : Repository<IngredientsType>, IIngredientsTypeRepository
    {
        public IngredientsTypeRepository() : base("IngredientsType")
        {
        }

        protected override IngredientsType MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("IngredientsTypeId"));
            string name = reader.GetString(reader.GetOrdinal("TypeName"));

            return IngredientsType.Reconstitute(id, name);
        }

        protected override string BuildInsertSql(IngredientsType entity)
        {
            return $"INSERT INTO {_tableName} (TypeName) VALUES (@TypeName)";
        }

        protected override SqlParameter[] GetInsertParameters(IngredientsType entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@TypeName", entity.IngredientsTypeName)
            };
        }

        protected override string BuildUpdateSql(IngredientsType entity)
        {
            return $"UPDATE {_tableName} SET TypeName = @TypeName WHERE IngredientsTypeId = @IngredientsTypeId";
        }

        protected override SqlParameter[] GetUpdateParameters(IngredientsType entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@TypeName", entity.IngredientsTypeName),
                new SqlParameter("@IngredientsTypeId", entity.GetId())
            };
        }

        public async Task<IngredientsType?> GetByNameAsync(string typeName)
        {
            const string sql = "SELECT IngredientsTypeId, TypeName FROM IngredientsType WHERE TypeName = @TypeName";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@TypeName", typeName)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }
    }
}
