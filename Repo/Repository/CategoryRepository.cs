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

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            int? parentCategoryId = reader.IsDBNull(reader.GetOrdinal("ParentCategoryId"))
                ? (int?)null
                : reader.GetInt32(reader.GetOrdinal("ParentCategoryId"));

            string categoryName = reader.GetString(reader.GetOrdinal("CategoryName"));
            int categoryTypeId = reader.GetInt32(reader.GetOrdinal("CategoryTypeId"));
            int accountId = reader.GetInt32(reader.GetOrdinal("AccountId"));

            return Category.Reconstitute(
                id,
                isActive,
                parentCategoryId,
                categoryName,
                categoryTypeId,
                accountId
            );
        }

        protected override string BuildInsertSql(Category entity)
        {
            return $"INSERT INTO {_tableName} (CategoryName, ParentCategoryId, CategoryTypeId, AccountId, CreatedAt, IsActive) " +
            $"VALUES (@CategoryName, @ParentCategoryId, @CategoryTypeId, @AccountId, GETDATE(), 1)";
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
            return $"UPDATE {_tableName} SET CategoryName = @CategoryName, ParentCategoryId = @ParentCategoryId, CategoryTypeId = @CategoryTypeId, IsActive = @IsActive " +
                   $"WHERE CategoriesId = @CategoriesId AND AccountId = @AccountId"; // <<-- AccountId adicionado ao WHERE
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
                new SqlParameter("@IsActive", entity.IsActive),
                new SqlParameter("@AccountId", entity.AccountId),
                new SqlParameter("@CategoriesId", entity.GetId())
            };
        }

        private const string SelectColumns = "CategoriesId, CategoryName, ParentCategoryId, CategoryTypeId, AccountId, CreatedAt, LastUpdatedAt, IsActive";

        public async Task<Category?> GetCategoryByNameAndAccount(string categoryName, int accountId)
        {
            // Consulta SQL: Define a lógica de busca. Seleciona as colunas necessárias para o construtor
            // O @ é o mecanismo padrão no C# e SQL Server para lidar com variáveis de entrada de forma segura e eficiente.
            string sql = $@" SELECT {SelectColumns} 
                             FROM {_tableName} 
                             WHERE CategoryName = @CategoryName AND AccountId = @AccountId AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CategoryName", categoryName),
                new SqlParameter("@AccountId", accountId)
            };

            try
            {
                return await ExecuteSingleAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetCategoryByName: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Category>> GetRootCategoriesByAccount(int accountId)
        {
            string sql = $@" SELECT {SelectColumns} 
                             FROM {_tableName} 
                             WHERE ParentCategoryId IS NULL AND AccountId = @AccountId AND IsActive = 1";

            SqlParameter paramAccountId = new SqlParameter("@AccountId", accountId);

            try
            {
                return (await ExecuteListAsync(sql, paramAccountId)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetRootCategoriesByAccount: {ex.Message}");
                throw;
            }
        }

        public async Task<Category?> ReadByIdAndAccountAsync(int id, int accountId)
        {
            string sql = $@" SELECT {SelectColumns} FROM {_tableName} 
                             WHERE CategoriesId = @CategoriesId AND AccountId = @AccountId AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CategoriesId", id),
                new SqlParameter("@AccountId", accountId)
            };

            try
            {
                return await ExecuteSingleAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório ReadByIdAndAccount: {ex.Message}");
                throw;
            }
        }

        public async Task<Category?> GetByIdWithSubCategories(int categoryId, int accountId)
        {
            Category? parentCategory = await ReadByIdAndAccountAsync(categoryId, accountId);

            if (parentCategory == null)
            {
                return null;
            }

            string subCategorySql = $@" SELECT {SelectColumns} 
                                    FROM {_tableName} 
                                    WHERE ParentCategoryId = @ParentId AND AccountId = @AccountId AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@ParentId", categoryId),
                new SqlParameter("@AccountId", accountId)
            };

            try
            {
                IEnumerable<Category> subCategories = await ExecuteListAsync(subCategorySql, parameters);
                parentCategory.SetSubCategories(subCategories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetByIdWithSubCategories: {ex.Message}");
                throw;
            }

            return parentCategory;
        }
    }
}
