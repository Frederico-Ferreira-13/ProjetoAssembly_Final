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
        public FavoritesRepository() : base("Favorites")
        {
        }

        protected override Favorites MapFromReader(SqlDataReader reader)
        {
            int id = Convert.ToInt32(reader["FavoritesId"]);
            int userId = Convert.ToInt32(reader["UserId"]);
            int recipesId = Convert.ToInt32(reader["RecipesId"]);

            DateTime createdAtFav = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
            bool isActiveFav = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            var favorite = Favorites.Reconstitute(id, userId, recipesId, createdAtFav, isActiveFav);

            string? imageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl"))
                        ? "default.jpg"
                        : reader.GetString(reader.GetOrdinal("ImageUrl"));

            favorite.Recipe = Recipes.Reconstitute(
                id: recipesId,
                userId: userId,
                categoriesId: reader.IsDBNull(reader.GetOrdinal("CategoriesId")) ? 0 : Convert.ToInt32(reader["CategoriesId"]),
                difficultyId: reader.IsDBNull(reader.GetOrdinal("DifficultyId")) ? 0 : Convert.ToInt32(reader["DifficultyId"]),
                title: reader.GetString(reader.GetOrdinal("Title")),
                instructions: reader.GetString(reader.GetOrdinal("Instructions")),
                prepTimeMinutes: Convert.ToInt32(reader["PrepTimeMinutes"]),
                cookTimeMinutes: Convert.ToInt32(reader["CookTimeMinutes"]),
                servings: reader.GetString(reader.GetOrdinal("Servings")),
                imageUrl: imageUrl,
                createdAt: DateTime.Now,
                lastUpdatedAt: null,
                isActive: true
            );

            return favorite;
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
            string sql = @"
                SELECT f.*, 
                       r.Title, r.Instructions, r.PrepTimeMinutes, 
                       r.CookTimeMinutes, r.Servings, r.CategoriesId, r.DifficultyId,
                       r.ImageUrl
                FROM Favorites f
                INNER JOIN Recipes r ON f.RecipesId = r.RecipesId
                WHERE f.UserId = @UserId AND f.IsActive = 1";
            
            SqlParameter param = new SqlParameter("@UserId", userId);

            return await ExecuteListAsync(sql, param);
        }

        public async Task<bool> ExistsAsync(int userId, int recipeId)
        {
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE UserId = @UserId AND RecipesId = @RecipesId AND IsActive = 1";
            SqlParameter[] paramsList =
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipesId", recipeId)
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

        public async Task DeactivateFavoriteAsync(int recipeId, int userId)
        {
            string sql = $"DELETE FROM {_tableName} WHERE UserId = @UserId AND RecipesId = @RecipeId";

            SqlParameter[] p =
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipeId", recipeId)
            };

            await SQL.ExecuteScalarAsync(sql, p);
        }
    }
}
