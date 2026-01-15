using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class UsersRepository : Repository<Users>, IUsersRepository
    {
        private const string SelectColumns = "UserId, UserName, Email, PasswordHash, Salt, " +
            "IsApproved, UsersRoleId, AccountId, CreatedAt, LastUpdatedAt, IsActive";

        public UsersRepository() : base("Users")
        {
        }

        protected override string PrimaryKeyName => "UserId";

        protected override Users MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("UserId"));

            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            DateTime? lastUpdatedAt = reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt"))
                       ? (DateTime?)null
                       : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            string userName = reader.GetString(reader.GetOrdinal("UserName"));
            string email = reader.GetString(reader.GetOrdinal("Email"));

            string passwordHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
            string salt = reader.GetString(reader.GetOrdinal("Salt"));

            int usersRoleId = reader.GetInt32(reader.GetOrdinal("UsersRoleId"));

            bool isApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved"));
            int accountId = reader.GetInt32(reader.GetOrdinal("AccountId"));

            return Users.Reconstitute(
                id,
                userName,
                email,
                passwordHash,
                salt,
                isApproved,
                usersRoleId,
                accountId,
                createdAt,
                lastUpdatedAt,
                isActive
            );
        }

        protected override string BuildInsertSql(Users model)
        {
            return $"INSERT INTO {_tableName} (UserName, Email, PasswordHash, Salt, UsersRoleId, IsApproved, AccountId, CreatedAt, IsActive) " +
                    $"VALUES (@UserName, @Email, @PasswordHash, @Salt, @UsersRoleId, @IsApproved, @AccountId, GETDATE(), 1)";
        }

        protected override SqlParameter[] GetInsertParameters(Users model)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@UserName", model.UserName),
                new SqlParameter("@Email", model.Email),
                new SqlParameter("@PasswordHash", model.PasswordHash),
                new SqlParameter("@Salt", model.Salt),
                new SqlParameter("@UsersRoleId", model.UsersRoleId),
                new SqlParameter("@IsApproved", model.IsApproved),
                new SqlParameter("@AccountId", model.AccountId)
            };
        }

        protected override string BuildUpdateSql(Users model)
        {
            return $"UPDATE {_tableName} SET UserName = @UserName, Email = @Email, PasswordHash = @PasswordHash, " +
                   $"Salt = @Salt, UsersRoleId = @UsersRoleId, IsApproved = @IsApproved, AccountId = @AccountId, " +
                   $"LastUpdatedAt = GETDATE(), IsActive = @IsActive WHERE UserId = @Id";
        }

        protected override SqlParameter[] GetUpdateParameters(Users model)
        {
            List<SqlParameter> parameters = new List<SqlParameter>(GetInsertParameters(model));

            parameters.Add(new SqlParameter("@Id", model.GetId()));
            parameters.Add(new SqlParameter("@IsActive", model.IsActive));

            return parameters.ToArray();
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE UserId = @Id AND IsActive = 1";
            object? result = await SQL.ExecuteScalarAsync(sql, new SqlParameter("@Id", id));
            return result != null && Convert.ToInt32(result) > 0;
        }

        public async Task<Users?> GetByEmailAsync(string email)
        {
            string sql = $"SELECT {SelectColumns} FROM {_tableName} WHERE Email = @Email AND IsActive = 1";
            return await ExecuteSingleAsync(sql, new SqlParameter("@Email", email));
        }

        public async Task<Users?> GetUserByUserNameAsync(string userName)
        {
            string sql = $"SELECT {SelectColumns} FROM {_tableName} WHERE UserName = @UserName AND IsActive = 1";
            return await ExecuteSingleAsync(sql, new SqlParameter("@UserName", userName));
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        {
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE Email = @Email AND IsActive = 1";
            var parameters = new List<SqlParameter> { new SqlParameter("@Email", email) };

            if (excludeId.HasValue)
            {
                sql += " AND UserId != @ExcludeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            object? result = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());
            return result != null && Convert.ToInt32(result) > 0;
        }

        public async Task<bool> ExistsByUserNameAsync(string userName, int? excludeId = null)
        {
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE UserName = @UserName AND IsActive = 1";
            var parameters = new List<SqlParameter> { new SqlParameter("@UserName", userName) };

            if (excludeId.HasValue)
            {
                sql += " AND UserId != @ExcludeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            object? result = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());
            return result != null && Convert.ToInt32(result) > 0;
        }

        public async Task<List<Users>> GetByApprovalStatusAsync(bool isApproved)
        {
            string sql = $"SELECT {SelectColumns} FROM {_tableName} WHERE IsApproved = @IsApproved AND IsActive = 1";
            var result = await ExecuteListAsync(sql, new SqlParameter("@IsApproved", isApproved));
                        
            return result.ToList();
        }
    }
}
