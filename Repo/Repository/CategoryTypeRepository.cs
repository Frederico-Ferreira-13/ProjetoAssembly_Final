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
        protected override string PrimaryKeyName => "CategoryTypeId";
        public CategoryTypeRepository() : base("CategoryType") { }

        protected override CategoryType MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("CategoryTypeId"));
            string name = reader.GetString(reader.GetOrdinal("TypeName"));            

            return CategoryType.Reconstitute(id, name);
        }

        protected override string BuildInsertSql(CategoryType entity)
        {
            return $"INSERT INTO {_tableName} (TypeName) VALUES (@TypeName)";
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
            return $"UPDATE {_tableName} SET TypeName = @TypeName WHERE CategoryTypeId = @CategoryTypeId";
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
            string sql = @"
                SELECT CategoryTypeId, TypeName 
                FROM CategoryType 
                WHERE TypeName = @TypeName";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@TypeName", typeName)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }
    }
}
