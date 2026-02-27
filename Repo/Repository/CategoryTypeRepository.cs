using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class CategoryTypeRepository : Repository<CategoryType>, ICategoryTypeRepository
    {        
        public CategoryTypeRepository() : base("CategoryType") { }

        protected override string PrimaryKeyName => "CategoryTypeId";

        protected override CategoryType MapFromReader(SqlDataReader reader)
        {
            return new CategoryType(
                id: reader.GetInt32(reader.GetOrdinal("CategoryTypeId")),
                name: reader.GetString(reader.GetOrdinal("TypeName"))
            );
        }

        protected override string BuildInsertSql(CategoryType entity)
        {
            return $@"INSERT INTO {_tableName} (TypeName)
                      VALUES (@TypeName)";
        }

        protected override SqlParameter[] GetInsertParameters(CategoryType entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@TypeName", entity.TypeName)
            };
        }

        protected override string BuildUpdateSql(CategoryType entity)
        {
            return $@"UPDATE {_tableName}
                      SET TypeName = @TypeName
                      WHERE CategoryTypeId = @CategoryTypeId";
        }

        protected override SqlParameter[] GetUpdateParameters(CategoryType entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@TypeName", entity.TypeName),
                new SqlParameter("@CategoryTypeId", entity.GetId())
            };
        }

        public async Task<CategoryType?> GetByNameAsync(string typeName)
        {
            string sql = $@"SELECT CategoryTypeId, TypeName
                            FROM {_tableName}
                            WHERE TypeName = @TypeName";

            var parameters = new SqlParameter[] { new SqlParameter("@TypeName", typeName) };

            return await ExecuteSingleAsync(sql, parameters);
        }
    }
}
