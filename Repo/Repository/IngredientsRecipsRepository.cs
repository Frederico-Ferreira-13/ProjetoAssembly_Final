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
            int id = reader.GetInt32(reader.GetOrdinal("IngredientsRecipsId"));
            int recipesId = reader.GetInt32(reader.GetOrdinal("RecipesId"));
            int ingredientsId = reader.GetInt32(reader.GetOrdinal("IngredientsId"));
            decimal quantityValue = reader.GetDecimal(reader.GetOrdinal("QuantityValue"));
            string unit = reader.GetString(reader.GetOrdinal("Unit"));

            return IngredientsRecips.Reconstitute(
                id,               
                recipesId,
                ingredientsId,
                quantityValue,
                unit
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
            List<IngredientsRecips> items = new List<IngredientsRecips>();

            string sql = $@"SELECT IngredientsRecipsId, RecipesId, IngredientsId, QuantityValue, Unit
                            FROM {_tableName}
                            WHERE RecipesId = @RecipesId
                            ORDER BY IngredientsId";

            SqlParameter paramRecipesId = new SqlParameter("@RecipesId", recipeId);

            return (await ExecuteListAsync(sql, paramRecipesId)).ToList();
        }

        public async Task<bool> IsIngredientUsedInRecipeAsync(int recipeId, int ingredientId)
        {
            string sql = $@"SELECT COUNT(1)
                            FROM {_tableName}
                            WHERE RecipesId = @RecipesId AND IngredientsId = @IngredientsId";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipesId", recipeId),
                new SqlParameter("@IngredientsId", ingredientId)
            };

            var result = await SQL.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> IsIngredientUsedInAnyRecipeAsync(int ingredientId)
        {
            string sql = $@"SELECT COUNT(1)
                            FROM {_tableName}
                            WHERE IngredientsId = @IngredientsId";

            SqlParameter param = new SqlParameter("@IngredientsId", ingredientId);

            var result = await SQL.ExecuteScalarAsync(sql, param);
            return Convert.ToInt32(result) > 0;
        }
    }
}
