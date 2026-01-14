using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class IngredientsRepository : Repository<Ingredients>, IIngredientsRepository
    {

        public IngredientsRepository() : base("Ingredients") { }

        protected override Ingredients MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("IngredientsId"));


            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            string ingredientsName = reader.GetString(reader.GetOrdinal("IngredientName"));
            int ingredientsTypeId = reader.GetInt32(reader.GetOrdinal("IngredientsTypeId"));

            return Ingredients.Reconstitute(
                id,
                isActive,
                ingredientsName,
                ingredientsTypeId
            );
        }

        protected override string BuildInsertSql(Ingredients entity)
        {
            return $"INSERT INTO {_tableName} (IngredientName, IngredientsTypeId, IsActive) " +
                   $"VALUES (@IngredientName, @IngredientsTypeId, 1)";
        }

        protected override SqlParameter[] GetInsertParameters(Ingredients entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@IngredientName", entity.IngredientName),
                new SqlParameter("@IngredientsTypeId", entity.IngredientsTypeId),
            };
        }

        protected override string BuildUpdateSql(Ingredients entity)
        {
            return $"UPDATE {_tableName} SET IngredientName = @IngredientName, IngredientsTypeId = @IngredientsTypeId, IsActive = @IsActive " +
                   $"WHERE IngredientsId = @IngredientsId";
        }

        protected override SqlParameter[] GetUpdateParameters(Ingredients entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@IngredientName", entity.IngredientName),
                new SqlParameter("@IngredientsTypeId", entity.IngredientsTypeId),
                new SqlParameter("@IngredientsId", entity.GetId()),
                new SqlParameter("@IsActive", entity.IsActive)
            };
        }

        public async Task<List<Ingredients>> Search(string searchIngredient)
        {
            List<Ingredients> ingredients = new List<Ingredients>();

            SqlParameter paramSearch = new SqlParameter("@SearchIngredient", $"%{searchIngredient}%");

            string sql = $"SELECT * FROM {_tableName} WHERE IngredientName LIKE @SearchIngredient AND IsActive = 1 ORDER BY IngredientName ASC";

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramSearch))
                {
                    while (reader.Read())
                    {
                        ingredients.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório Search: {ex.Message}");
                throw;
            }

            return ingredients;
        }

        public async Task<Ingredients?> GetByNameAsync(string ingredientsName)
        {
            string sql = $"SELECT * FROM {_tableName} WHERE IngredientName = @Name AND IsActive = 1";

            SqlParameter paramName = new SqlParameter("@Name", ingredientsName);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramName))
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetByNameAsync: {ex.Message}");
                throw;
            }

            return null;
        }

        public async Task<bool> IsIngredientUnique(string ingredientUnique, int? excludeId = null)
        {
            string sql = $"SELECT COUNT(Id) FROM {_tableName} WHERE IngredientName = @IngredientName  AND IsActive = 1";

            SqlParameter paramName = new SqlParameter("@IngredientName", ingredientUnique);
            List<SqlParameter> parameters = new List<SqlParameter> { paramName };

            if (excludeId.HasValue && excludeId.Value > 0)
            {
                sql += " AND IngredientsId <> @ExcludeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            try
            {
                object result = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());

                int count = Convert.ToInt32(result);
                return count == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório IsIngredientUnique: {ex.Message}");
                throw;
            }
        }
    }
}
