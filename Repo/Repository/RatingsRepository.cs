using Contracts.Repository;
using Core.Model;
using Core.Model.ValueObjects;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class RatingsRepository : Repository<Ratings>, IRatingRepository
    {
        protected override string PrimaryKeyName => "RantingsId";
        public RatingsRepository() : base("Ratings") { }

        protected override Ratings MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("RatingsId"));
            int recipesId = reader.GetInt32(reader.GetOrdinal("RecipesId"));
            int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
            int ratingValueInt = reader.GetInt32(reader.GetOrdinal("RatingValue"));
            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
            
            StarRating starRating = StarRating.Create(ratingValueInt);

            return Ratings.Reconstitute(
                id,
                createdAt,                
                recipesId,
                userId,
                starRating
            );
        }

        protected override string BuildInsertSql(Ratings entity)
        {
            return @$"INSERT INTO {_tableName} (RecipesId, UserId, RatingValue, CreatedAt) 
                   VALUES (@RecipesId, @UserId, @RatingValue, GETDATE())";
        }

        protected override SqlParameter[] GetInsertParameters(Ratings entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@RatingValue", entity.RatingValue.Value)
            };
        }

        protected override string BuildUpdateSql(Ratings entity)
        {
            return $@"UPDATE {_tableName}
                      SET RatingValue = @RatingValue
                      WHERE RatingsId = @RatingsId";
        }

        protected override SqlParameter[] GetUpdateParameters(Ratings entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RatingValue", entity.RatingValue.Value),
                new SqlParameter("@RatingsId", entity.GetId())
            };
        }

        public async Task<Ratings?> GetRatingByUserIdAndRecipeIdAsync(int userId, int recipesId)
        {
            string sql = $@"SELECT RatingId, RecipesId, UserId, RatingValue, CreatedAt
                            FROM {_tableName}
                            WHERE UserId = @UserId AND RecipesId = @RecipesId";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipesId", recipesId)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }

        public async Task<List<Ratings>> GetRatingsByRecipeIdAsync(int recipeId)
        {
            List<Ratings> ratings = new List<Ratings>();

            string sql = $@"SELECT RatingsId, RecipesId, UserId, RatingValue, CreatedAt
                    FROM {_tableName}
                    WHERE RecipesId = @RecipeId
                    ORDER BY CreatedAt DESC";

            SqlParameter paramRecipeId = new SqlParameter("@RecipesId", recipeId);

            return (await ExecuteListAsync(sql, paramRecipeId)).ToList();
        }

        public async Task<List<Ratings>> GetAllRatingsByUserIdAsync(int userId)
        {
            string sql = $@"SELECT RatingsId, RecipesId, UserId, RatingValue, CreatedAt
                    FROM {_tableName}
                    WHERE UserId = @UserId
                    ORDER BY CreatedAt DESC";

            SqlParameter paramUserId = new SqlParameter("@UserId", userId);

            return (await ExecuteListAsync(sql, paramUserId)).ToList();
        }

        public async Task<double> GetAverageRatingAsync(int recipeId)
        {
            string sql = $@"SELECT AVG(CAST(RatingValue AS FLOAT)) 
                            FROM {_tableName} 
                            WHERE RecipesId = @RecipeId";

            SqlParameter param = new SqlParameter("@RecipeId", recipeId);

            var result = await SQL.ExecuteScalarAsync(sql, param);
            return result == DBNull.Value ? 0.0 : Convert.ToDouble(result);
        }

        public async Task<bool> ExistsByUserAndRecipeAsync(int recipeId, int userId)
        {
            string sql = $@"SELECT COUNT(1) FROM {_tableName} 
                 WHERE RecipesId = @RecipeId AND UserId = @UserId";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipeId", recipeId),
                new SqlParameter("@UserId", userId)
            };

            var result = await SQL.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }
    }
}
