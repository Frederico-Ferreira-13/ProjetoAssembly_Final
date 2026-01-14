using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class UserRoleRepository : Repository<UsersRole>, IUserRoleReposiotry
    {
        public UserRoleRepository() : base("UsersRole")
        {
        }

        protected override UsersRole MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("UserRoleId"));
            string name = reader.GetString(reader.GetOrdinal("RoleName"));
            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            return UsersRole.Reconstitute(id, isActive, name);
        }

        protected override string BuildInsertSql(UsersRole entity)
        {
            return $"INSERT INTO {_tableName} (RoleName) VALUES (@RoleName)";
        }

        protected override SqlParameter[] GetInsertParameters(UsersRole entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RoleName", entity.RoleName)
            };
        }

        protected override string BuildUpdateSql(UsersRole entity)
        {
            return $"UPDATE {_tableName} SET RoleName = @RoleName WHERE UserRoleId = @UserRoleId";
        }

        protected override SqlParameter[] GetUpdateParameters(UsersRole entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RoleName", entity.RoleName),
                new SqlParameter("@UserRoleId", entity.GetId())
            };
        }

        public async Task<UsersRole?> GetByNameAsync(string userRole)
        {
            string sql = $"SELECT UserRoleId, RoleName, IsActive FROM {_tableName} WHERE RoleName = @RoleName AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RoleName", userRole)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }
    }
}
