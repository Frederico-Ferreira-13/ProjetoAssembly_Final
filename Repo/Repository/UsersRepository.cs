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
        private const string SelectColumns = "UserId, Name, UserName, Email, PasswordHash, Salt, IsApproved, UsersRoleId, AccountId, CreatedAt, LastUpdatedAt, IsActive";

        public UsersRepository() : base("Users") { }

        protected override string PrimaryKeyName => "UserId";

        protected override Users MapFromReader(SqlDataReader reader)
        {
            return new Users(
                id: reader.GetInt32(reader.GetOrdinal("UserId")),
                name: reader.GetString(reader.GetOrdinal("Name")),
                userName: reader.GetString(reader.GetOrdinal("UserName")),
                email: reader.GetString(reader.GetOrdinal("Email")),
                passwordHash: reader.GetString(reader.GetOrdinal("PasswordHash")),
                salt: reader.GetString(reader.GetOrdinal("Salt")),
                isApproved: reader.GetBoolean(reader.GetOrdinal("IsApproved")),
                usersRoleId: reader.GetInt32(reader.GetOrdinal("UsersRoleId")),
                accountId: reader.GetInt32(reader.GetOrdinal("AccountId")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                lastUpdatedAt: reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt")),
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive"))
            );
        }

        protected override string BuildInsertSql(Users model)
        {
            return $@"INSERT INTO Users (Name, UserName, Email, PasswordHash, Salt, UsersRoleId, IsApproved, AccountId, CreatedAt, IsActive) 
              VALUES (@Name, @UserName, @Email, @PasswordHash, @Salt, @UsersRoleId, @IsApproved, @AccountId, GETDATE(), 1)";
        }

        protected override SqlParameter[] GetInsertParameters(Users model)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@Name", (object)model.Name ?? throw new ArgumentNullException(nameof(model.Name))),
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
            return @"UPDATE Users SET Name = @Name, UserName = @UserName, Email = @Email, PasswordHash = @PasswordHash, 
                     Salt = @Salt, UsersRoleId = @UsersRoleId, IsApproved = @IsApproved, AccountId = @AccountId, 
                     LastUpdatedAt = GETDATE(), IsActive = @IsActive 
                     WHERE UserId = @Id";
        }

        protected override SqlParameter[] GetUpdateParameters(Users model)
        {
            var parameters = new List<SqlParameter>(GetInsertParameters(model))
            {
                new SqlParameter("@Id", model.GetId()),
                new SqlParameter("@IsActive", model.IsActive)
            };
            return parameters.ToArray();
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            var user = await ReadByIdAsync(id, onlyActive: true);
            return user != null;
        }

        public async Task<Users?> GetByEmailAsync(string email)
        {
            string sql = $"SELECT {SelectColumns} FROM Users WHERE Email = @Email AND IsActive = 1";
            return await ExecuteSingleAsync(sql, new SqlParameter("@Email", email));
        }

        public async Task<Users?> GetUserByUserNameAsync(string userName)
        {
            string sql = $"SELECT {SelectColumns} FROM Users WHERE UserName = @UserName AND IsActive = 1";
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

            var count = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(count) > 0;
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

            var count = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(count) > 0;
        }

        public async Task<List<Users>> GetByApprovalStatusAsync(bool isApproved)
        {
            string sql = $"SELECT {SelectColumns} FROM Users WHERE IsApproved = @IsApproved AND IsActive = 1";
            var parameters = new SqlParameter[] { new SqlParameter("@IsApproved", isApproved) };

            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();
        }
    }
}
