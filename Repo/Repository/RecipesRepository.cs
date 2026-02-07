using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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
            int prepTimeMinutes = Convert.ToInt32(reader["PrepTimeMinutes"]);
            int cookTimeMinutes = Convert.ToInt32(reader["CookTimeMinutes"]);
            string servings = reader.GetString(reader.GetOrdinal("Servings"));

            string imageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? "default.jpg" : reader.GetString(reader.GetOrdinal("ImageUrl"));

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
                imageUrl,
                createdAt,
                lastUpdatedAt,
                isActive
            );
        }

        protected override string BuildInsertSql(Recipes entity)
        {
            return $"INSERT INTO {_tableName} (UserId, CategoriesId, DifficultyId, Title, Instructions, PrepTimeMinutes, CookTimeMinutes, Servings, ImageUrl, CreatedAt, IsActive) " +
                    $"VALUES (@UserId, @CategoriesId, @DifficultyId, @Title, @Instructions, @PrepTimeMinutes, @CookTimeMinutes, @Servings, @ImageUrl, GETDATE(), 1)";
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
                new SqlParameter("@Servings", entity.Servings),
                new SqlParameter("@ImageUrl", (object)entity.ImageUrl ?? DBNull.Value)
            };
        }

        protected override string BuildUpdateSql(Recipes entity)
        {
            return $"UPDATE {_tableName} SET " +
                   "Title = @Title, " +
                   "Instructions = @Instructions, " +
                   "PrepTimeMinutes = @PrepTimeMinutes, " +
                   "CookTimeMinutes = @CookTimeMinutes, " +
                   "Servings = @Servings, " +
                   "CategoriesId = @CategoriesId, " +
                   "DifficultyId = @DifficultyId, " +
                   "ImageUrl = @ImageUrl, " +
                   "LastUpdatedAt = GETDATE() " +
                   "WHERE RecipesId = @RecipesId";
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
                new SqlParameter("@ImageUrl", (object)entity.ImageUrl ?? DBNull.Value), // Adicionado
                new SqlParameter("@RecipesId", entity.GetId())
            };
        }

        public async Task<List<Recipes>> GetUserIdRecipes(int userId)
        {
            List<Recipes> recipes = new List<Recipes>();

            string sql = $@"
                SELECT RecipesId, UserId, CategoriesId, DifficultyId, Title, Instructions, 
                       PrepTimeMinutes, CookTimeMinutes, Servings, ImageUrl, CreatedAt, LastUpdatedAt, IsActive
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
                    var recipe = MapFromReader(reader); // Lê as colunas base

                    // Preencher as propriedades extra
                    recipe.FavoriteCount = reader.GetInt32(reader.GetOrdinal("FavoritesCount"));
                    recipe.IsFavorite = reader.GetInt32(reader.GetOrdinal("IsFavorited")) == 1;

                    recipes.Add(recipe);
                }
            }
            return recipes;
        }

        public async Task<(IEnumerable<Recipes> Items, int TotalCount)> SearchRecipesAsync(string? search, int? categoryId, int page, int pageSize, int? currentUserId)
        {
            var recipes = new List<Recipes>();
            int totalCount = 0;

            string searchTerm = search?.Trim() ?? string.Empty;

            string sql = $@"
                SELECT r.*,
                       (SELECT COUNT(*) FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.IsActive = 1) as FavoritesCount,
                       CASE WHEN EXISTS (SELECT 1 FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.UserId = @UserId AND f.IsActive = 1)
                            THEN 1 ELSE 0 END as IsFavorited,
                       COUNT(*) OVER() as TotalCount
                FROM Recipes r
                WHERE r.IsActive = 1
                    AND (@CategoryId IS NULL OR r.CategoriesId = @CategoryId)
                    AND (
                        @Search IS NULL 
                        OR r.Title COLLATE Latin1_General_CI_AI LIKE '%' + @Search + '%'
                        OR DIFFERENCE(r.Title, @Search) >= 3
                    )
                ORDER BY
                    CASE WHEN r.Title COLLATE Latin1_General_CI_AI LIKE @Search + '%' THEN 0 ELSE 1 END, 
                    r.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] parameters =
            {
                new SqlParameter("@UserId", (object)currentUserId ?? DBNull.Value),
                new SqlParameter("@CategoryId", (object)categoryId ?? DBNull.Value),
                new SqlParameter("@Search", (object)search ?? DBNull.Value),
                new SqlParameter("@Offset", (page - 1) * pageSize),
                new SqlParameter("@PageSize", pageSize)
            };

            using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters))
            {
                while(reader.Read())
                {
                    if(totalCount == 0)
                    {
                        totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));                       
                    }

                    var recipe = MapFromReader(reader);
                    recipe.FavoriteCount = reader.GetInt32(reader.GetOrdinal("FavoritesCount"));
                    recipe.IsFavorite = reader.GetInt32(reader.GetOrdinal("IsFavorited")) == 1;
                    recipes.Add(recipe);
                }
            }

            return (recipes, totalCount);
        }

        public async Task<bool> AnyWithDifficultyIdAsync(int difficultyId)
        {
            string sql = @"
                SELECT TOP 1 1 
                FROM Recipes 
                WHERE DifficultyId = @DifficultyId 
                  AND IsActive = 1";

            var parameter = new SqlParameter("@DifficultyId", difficultyId);

            using var reader = await SQL.ExecuteQueryAsync(sql, parameter);

            return reader.HasRows;
        }

        public async Task<IEnumerable<Recipes>> GetPendingRecipesAsync()
        {
            string sql = @"
                SELECT r.*, u.UserName, u.Email
                FROM Recipes r
                LEFT JOIN Users u ON r.UserId = u.UserId
                WHERE r.IsApproved = 0 AND r.IsActive = 1
                ORDER BY r.CreatedAt DESC";

            var recipes = new List<Recipes>();

            using var reader = await SQL.ExecuteQueryAsync(sql);

            while (await reader.ReadAsync())
            {
                var recipe = Recipes.Reconstitute(
                    id: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                    userId: reader.GetInt32(reader.GetOrdinal("UserId")),
                    categoriesId: reader.GetInt32(reader.GetOrdinal("CategoriesId")),
                    difficultyId: reader.GetInt32(reader.GetOrdinal("DifficultyId")),
                    title: reader.GetString(reader.GetOrdinal("Title")),
                    instructions: reader.GetString(reader.GetOrdinal("Instructions")),
                    prepTimeMinutes: reader.GetInt16(reader.GetOrdinal("PrepTimeMinutes")),
                    cookTimeMinutes: reader.GetInt16(reader.GetOrdinal("CookTimeMinutes")),
                    servings: reader.GetString(reader.GetOrdinal("Servings")),
                    imageUrl: reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    lastUpdatedAt: reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt")),
                    isActive: reader.GetBoolean(reader.GetOrdinal("IsActive"))
                );

                string validateEmail = reader.IsDBNull(reader.GetOrdinal("Email"))
                                       ? "sistema@temp.com"
                                       : reader.GetString(reader.GetOrdinal("Email"));

                string fakeHash = new string('0', 60);
                string fakeSalt = new string('0', 16);

                recipe.SetUser(Users.Reconstitute(
                   id: reader.GetInt32(reader.GetOrdinal("UserId")),
                   userName: reader.GetString(reader.GetOrdinal("UserName")),
                   email: reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString(reader.GetOrdinal("Email")),
                   passwordHash: "",
                   salt: "",
                   isApproved: true,
                   usersRoleId: 1,
                   accountId: 1,
                   createdAt: DateTime.Now,
                   lastUpdatedAt: null,
                   isActive: true
                ));

                recipes.Add(recipe);
            }
            return recipes;
        }
    }
}
