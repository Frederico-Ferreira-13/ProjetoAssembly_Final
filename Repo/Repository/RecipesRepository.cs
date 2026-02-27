using Contracts.Repository;
using Core.Common;
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
        public RecipesRepository() : base("Recipes") { }

        protected override Recipes MapFromReader(SqlDataReader reader)
        {
            return new Recipes(
                id: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                userId: reader.GetInt32(reader.GetOrdinal("UserId")),
                categoriesId: reader.GetInt32(reader.GetOrdinal("CategoriesId")),
                difficultyId: reader.GetInt32(reader.GetOrdinal("DifficultyId")),
                title: reader.GetString(reader.GetOrdinal("Title")),
                instructions: reader.GetString(reader.GetOrdinal("Instructions")),
                prepTimeMinutes: reader.GetInt32(reader.GetOrdinal("PrepTimeMinutes")),
                cookTimeMinutes: reader.GetInt32(reader.GetOrdinal("CookTimeMinutes")),
                servings: reader.GetString(reader.GetOrdinal("Servings")),
                imageUrl: reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                lastUpdatedAt: reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt")),
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive"))
            );
        }

        protected override string BuildInsertSql(Recipes entity)
        {
            return $@"INSERT INTO {_tableName} (UserId, CategoriesId, DifficultyId, Title, Instructions, PrepTimeMinutes, CookTimeMinutes, Servings, ImageUrl, CreatedAt, IsActive, IsApproved)
                      VALUES (@UserId, @CategoriesId, @DifficultyId, @Title, @Instructions, @PrepTimeMinutes, @CookTimeMinutes, @Servings, @ImageUrl, GETDATE(), 1, @IsApproved)";
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
            return $@"UPDATE {_tableName} SET 
                      Title = @Title, 
                      Instructions = @Instructions, 
                      PrepTimeMinutes = @PrepTimeMinutes, 
                      CookTimeMinutes = @CookTimeMinutes, 
                      Servings = @Servings, 
                      CategoriesId = @CategoriesId, 
                      DifficultyId = @DifficultyId, 
                      ImageUrl = @ImageUrl, 
                      LastUpdatedAt = GETDATE(),
                      IsApproved = @IsApproved
                      WHERE RecipesId = @RecipesId";
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
                new SqlParameter("@ImageUrl", (object)entity.ImageUrl ?? DBNull.Value),
                new SqlParameter("@IsApproved", entity.IsApproved),
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
                WHERE UserId = @UserId AND IsActive = 1";

            var parameters = new SqlParameter[] { new SqlParameter("@UserId", userId) };

            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();           
        }

        public async Task<bool> ExistsByIdAsync(int recipeId)
        {
            var recipe = await ReadByIdAsync(recipeId);

            return recipe != null;
        }

        public async Task<IEnumerable<Recipes>> GetRecipesWithFavoritesAsync(int? currentUserId, int? categoryId)
        {            
            string sql = @"
                SELECT r.*,
                (SELECT COUNT(*) FROM Favorites f Where f.RecipesId = r.RecipesId AND f.IsActive = 1) as FavoritesCount,
                CASE WHEN EXISTS (SELECT 1 FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.UserId = @UserId AND f.IsActive = 1)
                    THEN 1 ELSE 0 END as IsFavorited
                FROM Recipes r
                WHERE (@CategoryId IS NULL OR r.CategoriesId = @CategoryId) AND r.IsActive = 1";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", (object)currentUserId ?? DBNull.Value),
                new SqlParameter("@CategoryId", (object)categoryId ?? DBNull.Value)
            };

            var result = await ExecuteListAsync(sql, parameters);
            var recipes = new List<Recipes>();

            foreach (var recipe in result)
            {
                recipe.FavoriteCount = 0;
                recipe.IsFavorite = false;
                recipes.Add(recipe);
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

            var result = await ExecuteListAsync(sql, parameters);

            foreach (var recipe in result)
            {
                recipe.FavoriteCount = 0;
                recipe.IsFavorite = false;
                recipes.Add(recipe);
            }

            totalCount = recipes.Count;

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
                WHERE r.IsApproved = 0 AND r.IsActive = 1";

            var result = await ExecuteListAsync(sql);

            var recipes = new List<Recipes>();

            foreach (var recipe in result)
            {
                var fakeUser = new Users(
                    id: recipe.UserId ?? 0,
                    name: "Utilizador",
                    userName: "Utilizador",
                    email: "",
                    passwordHash: "",
                    salt: "",
                    isApproved: true,
                    usersRoleId: 1,
                    accountId: 1,
                    createdAt: DateTime.Now,
                    lastUpdatedAt: null,
                    isActive: true
                );

                recipe.SetUser(fakeUser);
                recipes.Add(recipe);
            }

            return recipes;
        }
    }
}
