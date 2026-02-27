using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class IngredientsRecipsRepository : Repository<IngredientsRecips>, IIngredientsRecipsRepository
    {
        protected override string PrimaryKeyName => "IngredientsRecipesId";
        public IngredientsRecipsRepository() : base("IngredientsRecipes") { }

        protected override IngredientsRecips MapFromReader(SqlDataReader reader)
        {
            return new IngredientsRecips(
                id: reader.GetInt32(reader.GetOrdinal("IngredientsRecipsId")),
                recipesId: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                ingredientsId: reader.GetInt32(reader.GetOrdinal("IngredientsId")),
                quantityValue: reader.GetDecimal(reader.GetOrdinal("QuantityValue")),
                unit: reader.GetString(reader.GetOrdinal("Unit"))
            );
        }

        protected override string BuildInsertSql(IngredientsRecips entity)
        {
            return @$"INSERT INTO {_tableName} (RecipesId, IngredientsId, QuantityValue, Unit)
                        VALUES (@RecipesId, @IngredientsId, @QuantityValue, @Unit)";
        }

        protected override SqlParameter[] GetInsertParameters(IngredientsRecips entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@IngredientsId", entity.IngredientsId),
                new SqlParameter("@QuantityValue", entity.QuantityValue),
                new SqlParameter("@Unit", entity.Unit)
            };
        }

        protected override string BuildUpdateSql(IngredientsRecips entity)
        {
            return @$"UPDATE {_tableName}
                      SET QuantityValue = @QuantityValue, 
                          Unit = @Unit
                      WHERE IngredientsRecipsId = @IngredientsRecipsId";
        }

        protected override SqlParameter[] GetUpdateParameters(IngredientsRecips entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@QuantityValue", entity.QuantityValue),
                new SqlParameter("@Unit", entity.Unit),
                new SqlParameter("@IngredientsRecipsId", entity.GetId())
            };
        }

        public async Task<List<IngredientsRecips>> GetByRecipesIdAsync(int recipeId)
        {
            string sql = $@"SELECT IngredientsRecipsId, RecipesId, IngredientsId, QuantityValue, Unit
                            FROM {_tableName}
                            WHERE RecipesId = @RecipesId
                            ORDER BY IngredientsId";

            var parameters = new SqlParameter[] { new SqlParameter("@RecipesId", recipeId) };

            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();
        }

        public async Task<bool> IsIngredientUsedInRecipeAsync(int recipeId, int ingredientId)
        {
            string sql = $@"SELECT COUNT(1)
                            FROM {_tableName}
                            WHERE RecipesId = @RecipesId AND IngredientsId = @IngredientsId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipesId", recipeId),
                new SqlParameter("@IngredientsId", ingredientId)
            };

            var count = await SQL.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(count) > 0;
        }

        public async Task<bool> IsIngredientUsedInAnyRecipeAsync(int ingredientId)
        {
            string sql = $@"SELECT COUNT(1)
                            FROM {_tableName}
                            WHERE IngredientsId = @IngredientsId";

            var parameters = new SqlParameter[] { new SqlParameter("@IngredientsId", ingredientId) };

            var result = await SQL.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }
    }
}
