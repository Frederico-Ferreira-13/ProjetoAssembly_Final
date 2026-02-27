using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class FavoritesRepository : Repository<Favorites>, IFavoritesRepository
    {
        protected override string PrimaryKeyName => "FavoritesId";
        public FavoritesRepository() : base("Favorites") { }

        protected override Favorites MapFromReader(SqlDataReader reader)
        {
            return new Favorites(
                id: reader.GetInt32(reader.GetOrdinal("FavoritesId")),
                userId: reader.GetInt32(reader.GetOrdinal("UserId")),
                recipesId: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            );
        }

        protected override string BuildInsertSql(Favorites entity)
        {
            return $@"INSERT INTO {_tableName} (UserId, RecipesId, CreatedAt)
                      VALUES (@UserId, @RecipesId, GETDATE())";
        }

        protected override SqlParameter[] GetInsertParameters(Favorites entity)
        {

            return new SqlParameter[]
            {
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@RecipesId", entity.RecipesId),
            };
        }

        protected override string BuildUpdateSql(Favorites entity)
        {            
            return string.Empty;           
        }

        protected override SqlParameter[] GetUpdateParameters(Favorites entity)
        {
            return Array.Empty<SqlParameter>();
        }

        public async Task<IEnumerable<Favorites>> GetByUserIdAsync(int userId)
        {
            string sql = @"
                SELECT f.FavoritesId, f.UserId, f.RecipesId, f.CreatedAt,
                       r.Title, r.ImageUrl, r.CategoriesId, r.DifficultyId,
                       r.PrepTimeMinutes, r.CookTimeMinutes, r.Servings
                FROM Favorites f
                INNER JOIN Recipes r ON f.RecipesId = r.RecipesId
                WHERE f.UserId = @UserId";


            var parameters = new SqlParameter[] { new SqlParameter("@UserId", userId) };

            return await ExecuteListAsync(sql, parameters);
        }

        public async Task<bool> ExistsAsync(int userId, int recipeId)
        {
            string sql = $@"SELECT COUNT(1) FROM {_tableName}
                            WHERE UserId = @UserId AND RecipesId = @RecipesId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipesId", recipeId)
            };

            var count = await SQL.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(count) > 0;
        }

        public async Task<int> GetCountByRecipeIdAsync(int recipeId)
        {
            string sql = $@"SELECT COUNT(*) FROM {_tableName}
                            WHERE RecipesId = @RecipeId";

            var parameter = new SqlParameter("@RecipeId", recipeId);

            var result = await SQL.ExecuteScalarAsync(sql, parameter);
            return Convert.ToInt32(result);
        }

        public async Task DeleteFavoriteAsync(int userId, int recipeId)
        {
            string sql = $@"DELETE FROM {_tableName}
                            WHERE UserId = @UserId AND RecipesId = @RecipesId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipesId", recipeId)
            };

            await SQL.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<Favorites?> GetByUserAndRecipeAsync(int userId, int recipesId)
        {
            string sql = $@"SELECT * FROM FavoritesId, UserId, RecipesId, CreatedAt
                            FROM {_tableName}
                            WHERE UserId = @UserId AND RecipesId = @RecipesId";

            var parameters = new[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipesId", recipesId)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }
    }
}
