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
        public RatingsRepository() : base("Ratings") { }

        protected override Ratings MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("RatingId"));
            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            int recipesId = reader.GetInt32(reader.GetOrdinal("RecipesId"));
            int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
            int ratingValueInt = reader.GetInt32(reader.GetOrdinal("RatingValue"));

            StarRating starRating = StarRating.Create(ratingValueInt);

            return Ratings.Reconstitute(
                id,
                createdAt,
                isActive,
                recipesId,
                userId,
                starRating
            );
        }

        protected override string BuildInsertSql(Ratings entity)
        {
            return $"INSERT INTO {_tableName} (RecipesId, UserId, RatingValue, CreatedAt, IsActive) " +
                   $"VALUES (@RecipesId, @UserId, @RatingValue, GETDATE(), 1)";
        }

        protected override SqlParameter[] GetInsertParameters(Ratings entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@RatingValue", entity.RatingValue.Value) // Usamos o .Value ou o cast implícito
            };
        }

        protected override string BuildUpdateSql(Ratings entity)
        {
            return $"UPDATE {_tableName} SET RatingValue = @RatingValue" +
                   $"WHERE RatingId = @Id";
        }

        protected override SqlParameter[] GetUpdateParameters(Ratings entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RatingValue", entity.RatingValue.Value),
                new SqlParameter("@Id", entity.GetId())
            };
        }

        public async Task<Ratings?> GetRatingByUserIdAndRecipeIdAsync(int userId, int recipesId)
        {
            string sql = $@"
                SELECT RatingId, RecipesId, UserId, RatingValue, CreatedAt, IsActive
                FROM {_tableName}
                WHERE UserId = @UserId AND RecipesId = @RecipesId AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@RecipesId", recipesId)
            };

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters))
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetRatingByUserIdAndRecipeIdAsync: {ex.Message}");
                throw;
            }

            return null;
        }

        public async Task<List<Ratings>> GetRatingsByRecipeIdAsync(int recipeId)
        {
            List<Ratings> ratings = new List<Ratings>();

            string sql = $@"SELECT Id, RecipesId, UserId, RatingValue, CreatedAt, IsActive
                    FROM {_tableName}
                    WHERE RecipesId = @RecipeId AND IsActive = 1
                    ORDER BY CreatedAt DESC";

            SqlParameter paramRecipeId = new SqlParameter("@RecipesId", recipeId);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramRecipeId))
                {
                    while (reader.Read())
                    {
                        ratings.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetRatingsByRecipeIdAsync: {ex.Message}");
                throw;
            }

            return ratings;
        }

        public async Task<List<Ratings>> GetAllRatingsByUserIdAsync(int userId)
        {
            List<Ratings> ratings = new List<Ratings>();

            string sql = $@"
                    SELECT Id, RecipesId, UserId, RatingValue, CreatedAt, IsActive
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
                        ratings.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetAllRatingsByUserIdAsync: {ex.Message}");
                throw;
            }
            return ratings;
        }

        public async Task<double> GetAverageRatingAsync(int recipeId)
        {
            string sql = $@"
                SELECT AVG(CAST(RatingValue AS FLOAT)) 
                FROM {_tableName} 
                WHERE RecipesId = @RecipeId AND IsActive = 1";

            SqlParameter paramRecipeId = new SqlParameter("@RecipeId", recipeId);

            try
            {
                object? result = await SQL.ExecuteScalarAsync(sql, paramRecipeId);

                if (result == null || result == DBNull.Value)
                {
                    return 0.0;
                }

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetAverageRatingAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ExistsByUserAndRecipeAsync(int recipeId, int userId)
        {
            string sql = $"SELECT COUNT(RatingId) FROM {_tableName} " +
                 $"WHERE RecipesId = @RecipeId AND UserId = @UserId AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipeId", recipeId),
                new SqlParameter("@UserId", userId)
            };


            try
            {
                object result = await SQL.ExecuteScalarAsync(sql, parameters);

                if (result != null && result != DBNull.Value)
                {
                    int count = Convert.ToInt32(result);
                    return count > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório IsIngredientUnique: {ex.Message}");
                throw;
            }
        }
    }
}
