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
        public IngredientsRecipsRepository() : base("IngredientsRecipes") { }

        protected override IngredientsRecips MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("IngredientsRecipsId"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            int recipesId = reader.GetInt32(reader.GetOrdinal("RecipesId"));
            int ingredientsId = reader.GetInt32(reader.GetOrdinal("IngredientsId"));
            decimal quantityValue = reader.GetDecimal(reader.GetOrdinal("QuantityValue"));
            string unit = reader.GetString(reader.GetOrdinal("Unit"));

            return IngredientsRecips.Reconstitute(
                id,
                isActive,
                recipesId,
                ingredientsId,
                quantityValue,
                unit
            );
        }

        protected override string BuildInsertSql(IngredientsRecips entity)
        {
            return $"INSERT INTO {_tableName} (RecipesId, IngredientsId, QuantityValue, Unit, IsActive) " +
                   $"VALUES (@RecipesId, @IngredientsId, @QuantityValue, @Unit, 1)";
        }

        protected override SqlParameter[] GetInsertParameters(IngredientsRecips entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@IngredientsId", entity.IngredientsId),
                new SqlParameter("@QuantityValue", System.Data.SqlDbType.Decimal)
                {
                    Value = entity.QuantityValue,
                    Precision = 10,
                    Scale = 4
                },
                new SqlParameter("@Unit", entity.Unit)
            };
        }

        protected override string BuildUpdateSql(IngredientsRecips entity)
        {
            return $"UPDATE {_tableName} SET QuantityValue = @QuantityValue, Unit = @Unit " +
                   $"WHERE IngredientsRecipsId = @Id";
        }

        protected override SqlParameter[] GetUpdateParameters(IngredientsRecips entity)
        {
            return new SqlParameter[]
            {
                // Adicionada a tipagem Decimal para consistência e precisão
                new SqlParameter("@QuantityValue", System.Data.SqlDbType.Decimal)
                {
                    Value = entity.QuantityValue,
                    Precision = 10,
                    Scale = 4
                },
                new SqlParameter("@Unit", entity.Unit),
                new SqlParameter("@Id", entity.GetId())
            };
        }

        public async Task<List<IngredientsRecips>> GetByRecipesIdAsync(int recipeId)
        {
            List<IngredientsRecips> items = new List<IngredientsRecips>();

            string sql = $@"
                SELECT IngredientsRecipsId, RecipesId, IngredientsId, QuantityValue, Unit, IsActive
                FROM {_tableName}
                WHERE RecipesId = @RecipesId AND IsActive = 1
                ORDER BY IngredientsId";

            SqlParameter paramRecipesId = new SqlParameter("@RecipesId", recipeId);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramRecipesId))
                {
                    while (reader.Read())
                    {
                        items.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetByRecipeIdAsync: {ex.Message}");
                throw;
            }

            return items;
        }

        public async Task<bool> IsIngredientUsedInRecipeAsync(int recipeId, int ingredientId)
        {
            string sql = $@"
                    SELECT COUNT(Id)
                    FROM {_tableName}
                    WHERE RecipesId = @RecipesId AND IngredientsId = @IngredientsId AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RecipesId", recipeId),
                new SqlParameter("@IngredientsId", ingredientId)
            };

            try
            {
                object? result = await SQL.ExecuteScalarAsync(sql, parameters);

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório IsIngredientUsedInRecipeAsync: {ex.Message}");
                throw;
            }

            return false;
        }

        public async Task<bool> IsIngredientUsedInAnyRecipeAsync(int ingredientId)
        {
            string sql = $@"
                    SELECT COUNT(IngredientsRecipsId)
                    FROM {_tableName}
                    WHERE IngredientsId = @IngredientsId AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@IngredientsId", ingredientId)
            };

            try
            {
                object? result = await SQL.ExecuteScalarAsync(sql, parameters);

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório IsIngredientUsedInAnyRecipeAsync: {ex.Message}");
                throw;
            }

            return false;
        }
    }
}
