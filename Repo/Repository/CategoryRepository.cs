using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        protected override string PrimaryKeyName => "CategoryId";
        public CategoryRepository() : base("Category")
        {
        }

        protected override Category MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("CategoriesId"));
            int accountId = reader.GetInt32(reader.GetOrdinal("AccountId"));
            string categoryName = reader.GetString(reader.GetOrdinal("CategoryName"));
            int categoryTypeId = reader.GetInt32(reader.GetOrdinal("CategoryTypeId"));
            int? parentCategoryId = reader.IsDBNull(reader.GetOrdinal("ParentCategoryId"))
                ? (int?)null
                : reader.GetInt32(reader.GetOrdinal("ParentCategoryId"));            

            return Category.Reconstitute(
                id,                
                parentCategoryId,
                categoryName,
                categoryTypeId,
                accountId
            );
        }

        protected override string BuildInsertSql(Category entity)
        {
            return $@"INSERT INTO {_tableName} (CategoryName, ParentCategoryId, CategoryTypeId, AccountId)
                      VALUES (@CategoryName, @ParentCategoryId, @CategoryTypeId, @AccountId)";
        }

        protected override SqlParameter[] GetInsertParameters(Category entity)
        {
            object parentIdValue = entity.ParentCategoryId.HasValue
                                    ? (object)entity.ParentCategoryId.Value
                                    : DBNull.Value;

            return new SqlParameter[]
            {
                new SqlParameter("@CategoryName", entity.CategoryName),
                new SqlParameter("@ParentCategoryId", parentIdValue),
                new SqlParameter("@CategoryTypeId", entity.CategoryTypeId),
                new SqlParameter("@AccountId", entity.AccountId)
            };
        }

        protected override string BuildUpdateSql(Category entity)
        {
            return $@"UPDATE {_tableName} 
                      SET CategoryName = @CategoryName, 
                          ParentCategoryId = @ParentCategoryId, 
                          CategoryTypeId = @CategoryTypeId,
                      WHERE CategoriesId = @CategoriesId AND AccountId = @AccountId";
        }

        protected override SqlParameter[] GetUpdateParameters(Category entity)
        {
            object parentIdValue = entity.ParentCategoryId.HasValue
                                    ? (object)entity.ParentCategoryId.Value
                                    : DBNull.Value;

            return new SqlParameter[]
            {
                new SqlParameter("@CategoryName", entity.CategoryName),
                new SqlParameter("@ParentCategoryId", parentIdValue),
                new SqlParameter("@CategoryTypeId", entity.CategoryTypeId),                
                new SqlParameter("@AccountId", entity.AccountId),
                new SqlParameter("@CategoriesId", entity.GetId())
            };
        }
        
        public async Task<Category?> GetCategoryByNameAndAccount(string categoryName, int accountId)
        {
            // Consulta SQL: Define a lógica de busca. Seleciona as colunas necessárias para o construtor
            // O @ é o mecanismo padrão no C# e SQL Server para lidar com variáveis de entrada de forma segura e eficiente.
            string sql = $@" SELECT CategoriesId, CategoryName, ParentCategoryId, CategoryTypeId, AccountId
                             FROM {_tableName} 
                             WHERE CategoryName = @CategoryName AND AccountId = @AccountId";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CategoryName", categoryName),
                new SqlParameter("@AccountId", accountId)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }

        public async Task<List<Category>> GetRootCategoriesByAccount(int accountId)
        {
            string sql = $@" SELECT CategoriesId, CategoryName, ParentCategoryId, CategoryTypeId, AccountId
                             FROM {_tableName} 
                             WHERE ParentCategoryId IS NULL AND AccountId = @AccountId
                             ORDER BY CategoryName";

            return (await ExecuteListAsync(sql, new SqlParameter("@AccountId", accountId))).ToList();        
        }

        public async Task<Category?> ReadByIdAndAccountAsync(int id, int accountId)
        {
            string sql = $@" SELECT CategoriesId, CategoryName, ParentCategoryId, CategoryTypeId, AccountId
                             FROM {_tableName} 
                             WHERE CategoriesId = @CategoriesId AND AccountId = @AccountId";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CategoriesId", id),
                new SqlParameter("@AccountId", accountId)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }

        public async Task<Category?> GetByIdWithSubCategories(int categoryId, int accountId)
        {
            var parentCategory = await ReadByIdAndAccountAsync(categoryId, accountId);
            if (parentCategory == null)
            {
                return null;
            }

            string subSql = $@"SELECT CategoriesId, CategoryName, ParentCategoryId, CategoryTypeId, AccountId
                               FROM {_tableName}
                               WHERE ParentCategoryId = @ParentId AND AccountId = @AccountId
                               ORDER BY CategoryName";

            SqlParameter[] subParams = new SqlParameter[]
            {
                new SqlParameter("@ParentId", categoryId),
                new SqlParameter("@AccountId", accountId)
            };

            var subCategories = await ExecuteListAsync(subSql, subParams);
            parentCategory.SetSubCategories(subCategories);

            return parentCategory;
        }
    }
}
