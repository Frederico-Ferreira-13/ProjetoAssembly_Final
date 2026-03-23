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
        protected override string PrimaryKeyName => "IngredientsRecipsId";
        public IngredientsRecipsRepository() : base("IngredientsRecips") { }

        protected override IngredientsRecips MapFromReader(SqlDataReader reader)
        {
            var item = new IngredientsRecips(
                id: reader.GetInt32(reader.GetOrdinal("IngredientsRecipsId")),
                recipesId: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                ingredientsId: reader.GetInt32(reader.GetOrdinal("IngredientsId")),
                quantityValue: reader.GetDecimal(reader.GetOrdinal("QuantityValue")),
                unit: reader.GetString(reader.GetOrdinal("Unit"))
            );

            var detailOrdinal = reader.GetOrdinal("Detail");
            if (!reader.IsDBNull(detailOrdinal))
            {
                item.Update(item.QuantityValue, item.Unit, reader.GetString(detailOrdinal));
            }

            if (HasColumn(reader, "IngredientName"))
            {
                // Criamos um objeto Ingredient "fake" apenas com o nome para mostrar na UI
                item.SetIngredient(new Ingredients(
                    id: item.IngredientsId,
                    ingredientName: reader.GetString(reader.GetOrdinal("IngredientName")),
                    ingredientsTypeId: 0 // Valor dummy
                ));
            }

            return item;
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

        protected override string BuildInsertSql(IngredientsRecips entity)
        {
            return @$"INSERT INTO {_tableName} (RecipesId, IngredientsId, QuantityValue, Unit, Detail)
              VALUES (@RecipesId, @IngredientsId, @QuantityValue, @Unit, @Detail)";
        }

        protected override SqlParameter[] GetInsertParameters(IngredientsRecips entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@IngredientsId", entity.IngredientsId),
                new SqlParameter("@QuantityValue", entity.QuantityValue),
                new SqlParameter("@Unit", entity.Unit),
                new SqlParameter("@Detail", (object)entity.Detail ?? DBNull.Value)
            };
        }

        protected override string BuildUpdateSql(IngredientsRecips entity)
        {
            return @$"UPDATE {_tableName}
                      SET QuantityValue = @QuantityValue, 
                          Unit = @Unit,
                          Detail = @Detail
                      WHERE IngredientsRecipsId = @IngredientsRecipsId";
        }

        protected override SqlParameter[] GetUpdateParameters(IngredientsRecips entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@QuantityValue", entity.QuantityValue),
                new SqlParameter("@Unit", entity.Unit),
                new SqlParameter("@Detail", (object)entity.Detail ?? DBNull.Value),
                new SqlParameter("@IngredientsRecipsId", entity.GetId())
            };
        }

        public async Task<List<IngredientsRecips>> GetByRecipesIdAsync(int recipeId)
        {
            string sql = $@"SELECT IngredientsRecipsId, RecipesId, IngredientsId, QuantityValue, Unit, Detail
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

        public async Task<List<IngredientsRecips>> GetByRecipesIdWithNamesAsync(int recipeId)
        {
            string sql = $@"
                    SELECT ir.*, i.IngredientName
                    FROM IngredientsRecips ir
                    INNER JOIN Ingredients i on ir.IngredientsId = i.IngredientsId
                    WHERE ir.RecipesId = @RecipesId";

            var parameters = new SqlParameter[] { new SqlParameter("@RecipesId", recipeId) };

            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();
        }

        public async Task DeleteByRecipeIdAsync(int recipeId)
        {            
            string sql = $"DELETE FROM {_tableName} WHERE RecipesId = @RecipesId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipesId", recipeId)
            };
            
            await SQL.ExecuteNonQueryAsync(sql, parameters);
        }
    }
}
