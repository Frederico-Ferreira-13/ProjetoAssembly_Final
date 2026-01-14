using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class UsersRepository : Repository<Users>, IUsersRepository
    {
        public UsersRepository() : base("Users")
        {
        }

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

        protected override SqlParameter[] GetUpdateParameters(Users model)
        {
            List<SqlParameter> parameters = new List<SqlParameter>(GetInsertParameters(model));

            parameters.Add(new SqlParameter("@Id", model.GetId()));
            parameters.Add(new SqlParameter("@IsActive", model.IsActive));

            return parameters.ToArray();
        }

        protected override string BuildInsertSql(Users model)
        {
            return $"INSERT INTO {_tableName} (UserName, Email, PasswordHash, Salt, UsersRoleId, IsApproved, AccountId, CreatedAt, IsActive) " +
                    $"VALUES (@UserName, @Email, @PasswordHash, @Salt, @UsersRoleId, @IsApproved, @AccountId, GETDATE(), 1)";
        }

        protected override string BuildUpdateSql(Users model)
        {
            return $"UPDATE {_tableName} SET UserName = @UserName, Email = @Email, PasswordHash = @PasswordHash, " +
                   $"Salt = @Salt, UsersRoleId = @UsersRoleId, IsApproved = @IsApproved, AccountId = @AccountId, LastUpdatedAt = GETDATE(), IsActive = @IsActive " +
                   $"WHERE UserId = @Id";
        }

        private const string SelectColumns = "UserId, UserName, Email, PasswordHash, Salt, IsApproved, UsersRoleId, AccountId, CreatedAt, LastUpdatedAt, IsActive";

        public async Task<bool> ExistsByIdAsync(int id)
        {
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE UserId = @Id AND IsActive = 1";

            SqlParameter paramId = new SqlParameter("@Id", id);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramId))
                {
                    if (reader.Read())
                    {
                        // Ler o resultado da contagem
                        return reader.GetInt32(0) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErro SQL no Repositório ExistsByIdAsync: {ex.Message}");
                throw;
            }

            return false;
        }
        public async Task<Users?> GetByEmailAsync(string email)
        {
            string sql = $"SELECT {SelectColumns} FROM {_tableName} WHERE Email = @Email AND IsActive = 1";

            SqlParameter paramEmail = new SqlParameter("@Email", email);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramEmail))
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErro SQL no Repositório GetByEmail: {ex.Message}");
                throw;
            }
            return null;
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@Email", email)
            };

            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE Email = @Email AND IsActive = 1";

            if (excludeId.HasValue)
            {
                sql += $" AND UserId != @ExcludeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters.ToArray()))
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErro SQL no Repositório ExistsByEmail: {ex.Message}");
                throw;
            }

            return false;
        }

        public async Task<bool> ExistsByUserNameAsync(string userName, int? excludeId = null)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@UserName", userName)
            };

            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE UserName = @UserName AND IsActive = 1";

            if (excludeId.HasValue)
            {
                sql += $" AND UserId != @ExcludeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters.ToArray()))
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErro SQL no Repositório ExistsByUserName: {ex.Message}");
                throw;
            }

            return false;
        }

        public async Task<List<Users>> GetByApprovalStatusAsync(bool isApproved)
        {
            List<Users> users = new List<Users>();

            string sql = $"SELECT * FROM {_tableName} WHERE IsApproved = @IsApproved AND IsActive = 1";
            SqlParameter paramApproved = new SqlParameter("@IsApproved", isApproved);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramApproved))
                {
                    while (reader.Read())
                    {
                        users.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErro SQL no Repositório GetByApprovalStatus: {ex.Message}");
                throw;
            }

            return users;
        }

        public async Task<Users?> GetUserByUserNameAsync(string userName)
        {
            string sql = $"SELECT {SelectColumns} FROM {_tableName} WHERE UserName = @UserName AND IsActive = 1";

            SqlParameter paramUserName = new SqlParameter("@UserName", userName);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, paramUserName))
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErro SQL no Repositório GetUserByUserNameAsync: {ex.Message}");
                throw;
            }
            return null;
        }
    }
}
