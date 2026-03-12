using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class UserRoleRepository : Repository<UsersRole>, IUserRoleRepository
    {
        protected override string PrimaryKeyName => "UsersRoleId";
        public UserRoleRepository() : base("UsersRole") { }

        protected override UsersRole MapFromReader(SqlDataReader reader)
        {
            return new UsersRole(
                id: reader.GetInt32(reader.GetOrdinal("UsersRoleId")),
                name: reader.GetString(reader.GetOrdinal("RoleName"))
            );
        }

        protected override string BuildInsertSql(UsersRole entity)
        {
            return $@"INSERT INTO {_tableName} (RoleName)
                      VALUES (@RoleName)";
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
            return $@"UPDATE {_tableName}
                      SET RoleName = @RoleName 
                      WHERE UsersRoleId = @UsersRoleId";
        }

        protected override SqlParameter[] GetUpdateParameters(UsersRole entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RoleName", entity.RoleName),
                new SqlParameter("@UsersRoleId", entity.GetId())
            };
        }

        public async Task<UsersRole?> GetByNameAsync(string userRole)
        {
            string sql = $@"SELECT UsersRoleId, RoleName 
                            FROM {_tableName} 
                            WHERE RoleName = @RoleName";

            var parameters = new SqlParameter[] { new SqlParameter("@RoleName", userRole) };

            return await ExecuteSingleAsync(sql, parameters);
        }
    }
}
