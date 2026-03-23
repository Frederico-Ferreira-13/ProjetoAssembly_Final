using Contracts.Repository;
using Core.Model;
using Core.Model.ValueObjects;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class RatingsRepository : Repository<Ratings>, IRatingRepository
    {
        protected override string PrimaryKeyName => "RatingsId";
        public RatingsRepository() : base("Ratings") { }

        protected override Ratings MapFromReader(SqlDataReader reader)
        {
            return new Ratings(
                id: reader.GetInt32(reader.GetOrdinal("RatingsId")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                recipesId: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                userId: reader.GetInt32(reader.GetOrdinal("UserId")),
                ratingValue: StarRating.Create(reader.GetInt32(reader.GetOrdinal("RatingValue")))
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
            string sql = $@"SELECT RatingsId, RecipesId, UserId, RatingValue, CreatedAt
                            FROM {_tableName}
                            WHERE UserId = @UserId AND RecipesId = @RecipesId";

            var parameters = new SqlParameter[]
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
                    WHERE RecipesId = @RecipeId";

            var parameters = new SqlParameter[] { new SqlParameter("@RecipeId", recipeId) };

            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();
        }

        public async Task<List<Ratings>> GetAllRatingsByUserIdAsync(int userId)
        {
            string sql = $@"SELECT RatingsId, RecipesId, UserId, RatingValue, CreatedAt
                    FROM {_tableName}
                    WHERE UserId = @UserId";

            var parameters = new SqlParameter[] { new SqlParameter("@UserId", userId) };

            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();
        }

        public async Task<double> GetAverageRatingAsync(int recipeId)
        {
            string sql = $@"SELECT AVG(CAST(RatingValue AS FLOAT)) 
                            FROM {_tableName} 
                            WHERE RecipesId = @RecipeId";

            var parameter = new SqlParameter("@RecipeId", recipeId);

            var result = await SQL.ExecuteScalarAsync(sql, parameter);
            return result == DBNull.Value ? 0.0 : Convert.ToDouble(result);
        }

        public async Task<bool> ExistsByUserAndRecipeAsync(int recipeId, int userId)
        {
            string sql = $@"SELECT COUNT(1) FROM {_tableName} 
                 WHERE RecipesId = @RecipeId AND UserId = @UserId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipeId", recipeId),
                new SqlParameter("@UserId", userId)
            };

            var count = await SQL.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(count) > 0;
        }

        public async Task UpsertRatingAsync(int recipeId, int userId, int value)
        {            
            string sql = @"
                UPDATE Ratings SET RatingValue = @Value, CreatedAt = GETDATE() 
                WHERE RecipesId = @RecipeId AND UserId = @UserId;
        
                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO Ratings (RecipesId, UserId, RatingValue, CreatedAt, IsActive)
                    VALUES (@RecipeId, @UserId, @Value, GETDATE(), 1);
                END";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipeId", recipeId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Value", value)
            };

            await SQL.ExecuteNonQueryAsync(sql, parameters);
        }
    }
}
