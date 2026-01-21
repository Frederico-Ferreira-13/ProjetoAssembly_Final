using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class RecipesRepository : Repository<Recipes>, IRecipesRepository
    {
        protected override string PrimaryKeyName => "RecipesId";
        public RecipesRepository() : base("Recipes")
        {
        }

        protected override Recipes MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("RecipesId"));
            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            DateTime? lastUpdatedAt = reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt"))
                       ? (DateTime?)null
                       : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
            int categoriesId = reader.GetInt32(reader.GetOrdinal("CategoriesId"));
            int difficultyIdValue = reader.GetInt32(reader.GetOrdinal("DifficultyId"));
            string title = reader.GetString(reader.GetOrdinal("Title"));
            string instructions = reader.GetString(reader.GetOrdinal("Instructions"));
            int prepTimeMinutes = reader.GetInt32(reader.GetOrdinal("PrepTimeMinutes"));
            int cookTimeMinutes = reader.GetInt32(reader.GetOrdinal("CookTimeMinutes"));
            string servings = reader.GetString(reader.GetOrdinal("Servings"));

            return Recipes.Reconstitute(
                id,
                userId,
                categoriesId,
                difficultyIdValue,
                title,
                instructions,
                prepTimeMinutes,
                cookTimeMinutes,
                servings,
                createdAt,
                lastUpdatedAt,
                isActive
            );
        }

        protected override string BuildInsertSql(Recipes entity)
        {
            return $"INSERT INTO {_tableName} (UserId, CategoriesId, DifficultyId, Title, Instructions, PrepTimeMinutes, CookTimeMinutes, Servings, CreatedAt, IsActive) " +
                    $"VALUES (@UserId, @CategoriesId, @DifficultyId, @Title, @Instructions, @PrepTimeMinutes, @CookTimeMinutes, @Servings, GETDATE(), 1)";
        }

        protected override SqlParameter[] GetInsertParameters(Recipes entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@CategoriesId", entity.CategoriesId),
                new SqlParameter("@DifficultyId", entity.DifficultyId),
                new SqlParameter("@Title", entity.Title),
                new SqlParameter("@Instructions", entity.Instructions),
                new SqlParameter("@PrepTimeMinutes", entity.PrepTimeMinutes),
                new SqlParameter("@CookTimeMinutes", entity.CookTimeMinutes),
                new SqlParameter("@Servings", entity.Servings)
            };
        }

        protected override string BuildUpdateSql(Recipes entity)
        {
            return $"UPDATE {_tableName} SET Title = @Title, Instructions = @Instructions, " +
               $"PrepTimeMinutes = @PrepTimeMinutes, CookTimeMinutes = @CookTimeMinutes, " +
               $"Servings = @Servings, CategoriesId = @CategoriesId, DifficultyId = @DifficultyId, " +
               $"LastUpdatedAt = GETDATE() WHERE RecipesId = @RecipesId";
        }

        protected override SqlParameter[] GetUpdateParameters(Recipes entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@Title", entity.Title),
                new SqlParameter("@Instructions", entity.Instructions),
                new SqlParameter("@PrepTimeMinutes", entity.PrepTimeMinutes),
                new SqlParameter("@CookTimeMinutes", entity.CookTimeMinutes),
                new SqlParameter("@Servings", entity.Servings),
                new SqlParameter("@CategoriesId", entity.CategoriesId),
                new SqlParameter("@DifficultyId", entity.DifficultyId),

                new SqlParameter("@RecipesId", entity.GetId())
            };
        }

        public async Task<List<Recipes>> GetUserIdRecipes(int userId)
        {
            List<Recipes> recipes = new List<Recipes>();

            string sql = $@"
                SELECT RecipesId, UserId, CategoriesId, DifficultyId, Title, Instructions, 
                       PrepTimeMinutes, CookTimeMinutes, Servings, CreatedAt, LastUpdatedAt, IsActive
                FROM {_tableName} 
                WHERE UserId = @UserId AND IsActive = 1
                ORDER BY CreatedAt DESC";

            SqlParameter paramUserId = new SqlParameter("@UserId", userId);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramUserId))
                {
                    while (reader.Read())
                    {
                        recipes.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetByUserIdAsync: {ex.Message}");
                throw;
            }

            return recipes;
        }

        public async Task<bool> ExistsByIdAsync(int recipeId)
        {
            string sql = $@"
                SELECT TOP 1 1
                FROM {_tableName}
                WHERE RecipesId = @RecipesId AND IsActive = 1";

            SqlParameter paramId = new SqlParameter("@RecipesId", recipeId);
            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramId))
                {
                    return reader.HasRows;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório ExistsByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Recipes>> GetRecipesWithFavoritesAsync(int? currentUserId, int? categoryId)
        {
            var recipes = new List<Recipes>();
            string sql = @"
                SELECT r.*,
                (SELECT COUNT(*) FROM Favorites f Where f.RecipesId = r.RecipesId AND f.IsActive = 1) as FavoritesCount,
                CASE WHEN EXISTS (SELECT 1 FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.UserId = @UserId AND f.IsActive = 1)
                    THEN 1 ELSE 0 END as IsFavorited
                FROM Recipes r
                WHERE (@CategoryId IS NULL OR r.CategoriesId = @CategoryId) AND r.IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", (object)currentUserId ?? DBNull.Value),
                new SqlParameter("@CategoryId", (object)categoryId ?? DBNull.Value)
            };

            using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters))
            {
                while (reader.Read())
                {
                    var recipe = MapFromReader(reader);

                    // Preencher as propriedades extra
                    recipe.FavoriteCount = reader.GetInt32(reader.GetOrdinal("FavoriteCount"));
                    recipe.IsFavorited = reader.GetInt32(reader.GetOrdinal("IsFavorited")) == 1;

                    recipes.Add(recipe);
                }
            }
            return recipes;
        }
    }
}
