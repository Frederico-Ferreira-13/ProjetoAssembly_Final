using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class DifficultyRepository : Repository<Difficulty>, IDifficultyRepository
    {
        protected override string PrimaryKeyName => "DifficultyId";
        public DifficultyRepository() : base("Difficulty")
        {
        }

        protected override Difficulty MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("DifficultyId"));
            string name = reader.GetString(reader.GetOrdinal("DifficultyName"));
            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            return Difficulty.Reconstitute(id, name, isActive);
        }

        protected override string BuildInsertSql(Difficulty entity)
        {
            return $"INSERT INTO {_tableName} (DifficultyName) VALUES (@DifficultyName)";
        }

        protected override SqlParameter[] GetInsertParameters(Difficulty entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@DifficultyName", entity.DifficultyName)
            };
        }

        protected override string BuildUpdateSql(Difficulty entity)
        {
            return $"UPDATE {_tableName} SET DifficultyName = @DifficultyName WHERE DifficultyId = @DifficultyId";
        }

        protected override SqlParameter[] GetUpdateParameters(Difficulty entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@DifficultyName", entity.DifficultyName),
                new SqlParameter("@DifficultyId", entity.GetId())
            };
        }

        public async Task<Difficulty?> GetByNameAsync(string difficultyName)
        {
            const string sql = @"
                SELECT DifficultyId, DifficultyName, IsActive 
                FROM Difficulty 
                WHERE DifficultyName = @DifficultyName AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@DifficultyName", difficultyName)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }
    }
}
