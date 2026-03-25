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
    public class RecipesRepository : SoftDeleteRepository<Recipes>, IRecipesRepository
    {
        protected override string PrimaryKeyName => "RecipesId";
        public RecipesRepository() : base("Recipes") { }

        protected override Recipes MapFromReader(SqlDataReader reader)
        {
            try
            {
                var recipe = new Recipes(
                    id: Convert.ToInt32(reader["RecipesId"]),
                    userId: Convert.ToInt32(reader["UserId"]),
                    categoriesId: Convert.ToInt32(reader["CategoriesId"]),
                    difficultyId: Convert.ToInt32(reader["DifficultyId"]),
                    title: reader["Title"]?.ToString() ?? string.Empty,
                    instructions: reader["Instructions"]?.ToString() ?? string.Empty,
                    prepTimeMinutes: Convert.ToInt32(reader["PrepTimeMinutes"]),
                    cookTimeMinutes: Convert.ToInt32(reader["CookTimeMinutes"]),
                    servings: reader["Servings"]?.ToString() ?? string.Empty,
                    imageUrl: reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader["ImageUrl"].ToString(),
                    createdAt: Convert.ToDateTime(reader["CreatedAt"]),
                    lastUpdatedAt: reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt")) ? null : (DateTime?)Convert.ToDateTime(reader["LastUpdatedAt"]),
                    isActive: Convert.ToBoolean(reader["IsActive"]),
                    isApproved: HasColumn(reader, "IsApproved") ? Convert.ToBoolean(reader["IsApproved"]) : false
                );

                if (HasColumn(reader, "FavoritesCount") && !reader.IsDBNull(reader.GetOrdinal("FavoritesCount")))
                    recipe.FavoriteCount = Convert.ToInt32(reader["FavoritesCount"]);

                if (HasColumn(reader, "AverageRating") && !reader.IsDBNull(reader.GetOrdinal("AverageRating")))
                    recipe.AverageRating = Convert.ToDouble(reader["AverageRating"]);

                if (HasColumn(reader, "IsFavorited") && !reader.IsDBNull(reader.GetOrdinal("IsFavorited")))
                    recipe.IsFavorite = Convert.ToInt32(reader["IsFavorited"]) == 1;

                if (HasColumn(reader, "UserRating") && !reader.IsDBNull(reader.GetOrdinal("UserRating")))
                {
                    recipe.UserRating = Convert.ToInt32(reader["UserRating"]);
                }

                return recipe;
            }
            catch (Exception ex)
            {
                // Isto vai imprimir o erro exato na janela "Output" do Visual Studio
                System.Diagnostics.Debug.WriteLine($"ERRO NO MAPEAMENTO DE RECEITA: {ex.Message}");
                throw; // Relança para o Service apanhar
            }
        }

        private bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
                new SqlParameter("@RecipesId", entity.GetId()),
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@CategoriesId", entity.CategoriesId),
                new SqlParameter("@DifficultyId", entity.DifficultyId),
                new SqlParameter("@Title", entity.Title ?? (object)DBNull.Value),
                new SqlParameter("@Instructions", entity.Instructions ?? (object)DBNull.Value),
                new SqlParameter("@PrepTimeMinutes", entity.PrepTimeMinutes),
                new SqlParameter("@CookTimeMinutes", entity.CookTimeMinutes),
                new SqlParameter("@Servings", entity.Servings ?? (object)DBNull.Value),
                new SqlParameter("@ImageUrl", (object)entity.ImageUrl! ?? DBNull.Value),
                new SqlParameter("@IsApproved", entity.IsApproved)
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
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@CategoriesId", entity.CategoriesId),
                new SqlParameter("@DifficultyId", entity.DifficultyId),
                new SqlParameter("@Title", entity.Title),
                new SqlParameter("@Instructions", entity.Instructions),
                new SqlParameter("@PrepTimeMinutes", entity.PrepTimeMinutes),
                new SqlParameter("@CookTimeMinutes", entity.CookTimeMinutes),
                new SqlParameter("@Servings", entity.Servings),
                new SqlParameter("@ImageUrl", (object)entity.ImageUrl! ?? DBNull.Value),
                new SqlParameter("@IsApproved", entity.IsApproved)
            };
        }

        public async Task<int> GetUserRatingAsync(int recipeId, int userId)
        {
            string sql = @"
                SELECT RatingValue 
                FROM Ratings 
                WHERE RecipesId = @RecipeId AND UserId = @UserId AND IsActive = 1";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipeId", recipeId),
                new SqlParameter("@UserId", userId)
            };

            using (var reader = await SQL.ExecuteQueryAsync(sql, parameters))
            {
                if (reader.Read())
                {
                    return Convert.ToInt32(reader["RatingValue"]);
                }
            }

            return 0;
        }
        public async Task<List<Recipes>> GetUserIdRecipes(int userId)
        {
            List<Recipes> recipes = new List<Recipes>();

            string sql = $@"
                SELECT RecipesId, UserId, CategoriesId, DifficultyId, Title, Instructions, 
                       PrepTimeMinutes, CookTimeMinutes, Servings, ImageUrl, CreatedAt, 
                       LastUpdatedAt, IsActive, IsApproved
                FROM {_tableName} 
                WHERE UserId = @UserId AND IsActive = 1";

            var parameters = new SqlParameter[] { new SqlParameter("@UserId", userId) };
            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();           
        }

        public async Task UpdateRecipeAverageRatingAsync(int recipeId)
        {
            string sql = @"
                UPDATE Recipes 
                SET AverageRating = (
                    SELECT ISNULL(AVG(CAST(RatingValue AS FLOAT)), 0) 
                    FROM Ratings 
                    WHERE RecipesId = @RecipeId AND IsActive = 1
                )
                WHERE RecipesId = @RecipeId";

            var parameter = new SqlParameter("@RecipeId", recipeId);
            await SQL.ExecuteNonQueryAsync(sql, parameter);
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
                (SELECT COUNT(*) FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.IsActive = 1) as FavoriteCount,
                (SELECT ISNULL(AVG(CAST(RatingValue AS FLOAT)), 0) FROM Ratings rt WHERE rt.RecipesId = r.RecipesId AND rt.IsActive = 1) as AverageRating,
                CASE WHEN EXISTS (SELECT 1 FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.UserId = @UserId AND f.IsActive = 1)
                    THEN 1 ELSE 0 END as IsFavorited
                FROM Recipes r
                WHERE (@CategoryId IS NULL OR r.CategoriesId = @CategoryId) AND r.IsActive = 1 AND r.IsApproved = 1";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", (object)currentUserId ?? DBNull.Value),
                new SqlParameter("@CategoryId", (object)categoryId ?? DBNull.Value)
            };

            return await ExecuteListAsync(sql, parameters);
        }

        public async Task<(IEnumerable<Recipes> Items, int TotalCount)> SearchRecipesAsync(string? search, int? categoryId, 
            int page, int pageSize, int? currentUserId)
        {
            string sql = $@"
                SELECT r.*,
                       (SELECT COUNT(*) FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.IsActive = 1) as FavoriteCount,
                       (SELECT ISNULL(AVG(CAST(RatingValue AS FLOAT)), 0) FROM Ratings rt WHERE rt.RecipesId = r.RecipesId AND rt.IsActive = 1) as AverageRating,           
                       (SELECT TOP 1 RatingValue FROM Ratings rt WHERE rt.RecipesId = r.RecipesId AND rt.UserId = @UserId AND rt.IsActive = 1) as UserRating,
                       CASE WHEN EXISTS (SELECT 1 FROM Favorites f WHERE f.RecipesId = r.RecipesId AND f.UserId = @UserId AND f.IsActive = 1)
                            THEN 1 ELSE 0 END as IsFavorited,
                       COUNT(*) OVER() as TotalRows
                FROM Recipes r
                WHERE r.IsActive = 1 AND r.IsApproved = 1
                    AND (@CategoryId IS NULL OR r.CategoriesId = @CategoryId)
                    AND (@SearchText IS NULL OR r.Title COLLATE Latin1_General_CI_AI LIKE '%' + @SearchText + '%')
                ORDER BY r.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            SqlParameter[] parameters =
            {
                new SqlParameter("@UserId", (object)currentUserId! ?? DBNull.Value),
                new SqlParameter("@CategoryId", (object)categoryId! ?? DBNull.Value),
                new SqlParameter("@SearchText", (object)search! ?? DBNull.Value), // Mudamos o nome para @SearchText
                new SqlParameter("@Offset", (page - 1) * pageSize),
                new SqlParameter("@PageLimit", pageSize)
            };

            var items = new List<Recipes>();
            int totalCount = 0;

            using (var reader = await SQL.ExecuteQueryAsync(sql, parameters))
            {
                while (reader.Read())
                {
                    if(totalCount == 0 && HasColumn(reader, "TotalRows"))
                    {
                        totalCount = reader.GetInt32(reader.GetOrdinal("TotalRows"));
                    }

                    items.Add(MapFromReader(reader));
                }
            }

            return (items, totalCount);
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
                    id: recipe.UserId,
                    name: "Utilizador",
                    userName: "Utilizador",
                    email: "",
                    profilePicture: null,
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

        public async Task<List<IngredientsRecips>> GetIngredientsByRecipeIdAsync(int recipeId)
        {
            var ingredientsList = new List<IngredientsRecips>();

            string sql = @"
                SELECT ir.*, i.IngredientName, i.IngredientsTypeId
                FROM IngredientsRecips ir
                INNER JOIN Ingredients i ON ir.IngredientsId = i.IngredientsId
                WHERE ir.RecipesId = @RecipeId AND ir.IsActive = 1";

            var parameter = new SqlParameter("@RecipeId", recipeId);

            using (var reader = await SQL.ExecuteQueryAsync(sql, parameter))
            {
                while (reader.Read())
                {                    
                    var item = new IngredientsRecips(
                        id: Convert.ToInt32(reader["IngredientsRecipsId"]),
                        recipesId: Convert.ToInt32(reader["RecipesId"]),
                        ingredientsId: Convert.ToInt32(reader["IngredientsId"]),
                        quantityValue: Convert.ToDecimal(reader["QuantityValue"]),
                        unit: reader["Unit"]?.ToString() ?? string.Empty,
                        detail: reader["Detail"]?.ToString()
                    );
                   
                    var ingredientInfo = new Ingredients(
                        id: Convert.ToInt32(reader["IngredientsId"]),
                        ingredientName: reader["IngredientName"]?.ToString() ?? "Desconhecido",
                        ingredientsTypeId: Convert.ToInt32(reader["IngredientsTypeId"])
                    );
                    
                    item.SetIngredient(ingredientInfo);

                    ingredientsList.Add(item);
                }
            }
            return ingredientsList;
        }

        public async Task UpsertRecipeRatingAsync(int recipeId, int userId, int rating)
        {           
            string sql = @"
                IF EXISTS (SELECT 1 FROM Ratings WHERE RecipesId = @RecipeId AND UserId = @UserId)
                BEGIN
                    UPDATE Ratings 
                    SET RatingValue = @Rating
                    WHERE RecipesId = @RecipeId AND UserId = @UserId
                END
                ELSE
                BEGIN
                    INSERT INTO Ratings (RecipesId, UserId, RatingValue, CreatedAt, IsActive)
                    VALUES (@RecipeId, @UserId, @Rating, GETDATE(), 1)
                END";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipeId", recipeId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Rating", rating)
            };

            await SQL.ExecuteNonQueryAsync(sql, parameters);
        }
    }
}
