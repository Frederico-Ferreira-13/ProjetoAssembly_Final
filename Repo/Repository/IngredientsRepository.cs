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
        protected override string PrimaryKeyName => "IngredientsId";
        public IngredientsRepository() : base("Ingredients") { }

        protected override Ingredients MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("IngredientsId"));
            string ingredientsName = reader.GetString(reader.GetOrdinal("IngredientName"));
            int ingredientsTypeId = reader.GetInt32(reader.GetOrdinal("IngredientsTypeId"));

            return Ingredients.Reconstitute(id, ingredientsName, ingredientsTypeId);
        }

        protected override string BuildInsertSql(Ingredients entity)
        {
            return  @$"INSERT INTO {_tableName} (IngredientName, IngredientsTypeId)
                       VALUES (@IngredientName, @IngredientsTypeId)";
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
            return @$"UPDATE {_tableName} 
                      SET IngredientName = @IngredientName, 
                          IngredientsTypeId = @IngredientsTypeId,
                      WHERE IngredientsId = @IngredientsId";
        }

        protected override SqlParameter[] GetUpdateParameters(Ingredients entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@IngredientName", entity.IngredientName),
                new SqlParameter("@IngredientsTypeId", entity.IngredientsTypeId),
                new SqlParameter("@IngredientsId", entity.GetId()),                
            };
        }

        public async Task<List<Ingredients>> Search(string searchIngredient)
        {
            string sql = @$"SELECT IngredientsId, IngredientName, IngredientsTypeId
                            FROM {_tableName} 
                            WHERE IngredientName LIKE @SearchIngredient 
                            ORDER BY IngredientName ASC";

            SqlParameter param = new SqlParameter("@SearchIngredient", $"%{searchIngredient}%");

            return (await ExecuteListAsync(sql, param)).ToList();
        }

        public async Task<Ingredients?> GetByNameAsync(string ingredientsName)
        {
            string sql = $@"SELECT IngredientsId, IngredientName, IngredientsTypeId
                            FROM {_tableName}
                            WHERE IngredientName = @IngredientName";

            SqlParameter paramName = new SqlParameter("@IngredientName", ingredientsName);

            return await ExecuteSingleAsync(sql, paramName);
        }

        public async Task<bool> IsIngredientUnique(string ingredientUnique, int? excludeId = null)
        {
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE IngredientName = @IngredientName";

            var parameters = new List<SqlParameter> { new SqlParameter("@IngredientName", ingredientUnique) };

            if (excludeId.HasValue)
            {
                sql += " AND IngredientsId != @ExcludeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            var result = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(result) == 0;
        }
    }
}
