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
        public FavoritesRepository() : base("Favorites")
        {
        }

        protected override Favorites MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("FavoritesId"));
            int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
            int recipesId = reader.GetInt32(reader.GetOrdinal("RecipesId"));

            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            return Favorites.Reconstitute(
                id,
                userId,
                recipesId,
                createdAt,
                isActive
            );
        }

        protected override string BuildInsertSql(Favorites entity)
        {
            return $"INSERT INTO {_tableName} (UserId, RecipesId, CreatedAt, IsActive) " +
                $"VALUES (@UserId, @RecipesId, @CreatedAt, @IsActive)";
        }

        protected override SqlParameter[] GetInsertParameters(Favorites entity)
        {

            return new SqlParameter[]
            {
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@CreatedAt", entity.CreatedAt),
                new SqlParameter("@IsActive", entity.IsActive)

            };
        }

        protected override string BuildUpdateSql(Favorites entity)
        {
            return $"UPDATE {_tableName} SET IsActive = @IsActive WHERE FavoritesId = @Id";
        }

        protected override SqlParameter[] GetUpdateParameters(Favorites entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@IsActive", entity.IsActive),
                new SqlParameter("@Id", entity.GetId())
            };
        }

        public async Task<IEnumerable<Favorites>> GetByUserIdAsync(int userId)
        {
            string sql = $"SELECT * FROM {_tableName} WHERE UserId = @UserId AND IsActive = 1";
            SqlParameter param = new SqlParameter("@UserId", userId);

            return await ExecuteListAsync(sql, param);
        }

        public async Task<bool> ExistsAsync(int userId, int recipeId)
        {
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE UserId = @UserId AND RecipesId = @RecipesId AND IsActive = 1";
            SqlParameter[] paramsList =
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipeId", recipeId)
            };

            var result = await SQL.ExecuteScalarAsync(sql, paramsList);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<int> GetCountByRecipeIdAsync(int recipeId)
        {
            string sql = $"SELECT COUNT(*) FROM {_tableName} WHERE RecipesId = @recipeId AND IsActive = 1";
            SqlParameter param = new SqlParameter("@recipeId", recipeId);

            var result = await SQL.ExecuteScalarAsync(sql, param);
            return Convert.ToInt32(result);
        }
    }
}
