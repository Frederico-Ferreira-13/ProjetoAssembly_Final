using Core.Model;
using Contracts.Repository;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class AccountRepository : Repository<Account>, IAccountRepository
    {
        public AccountRepository() : base("Account")
        {
        }

        protected override Account MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("AccountId"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            string accountName = reader.GetString(reader.GetOrdinal("AccountName"));
            string subscriptionLevel = reader.GetString(reader.GetOrdinal("SubscriptionLevel"));

            int creatorUserId = reader.IsDBNull(reader.GetOrdinal("CreatorUserId")) ? 0
                : reader.GetInt32(reader.GetOrdinal("CreatorUserId"));

            return Account.Reconstitute(
                id,
                isActive,
                accountName,
                subscriptionLevel,
                creatorUserId
            );
        }

        protected override string BuildInsertSql(Account entity)
        {
            // Insere apenas os campos necessários (ID é IDENTITY)
            return $"INSERT INTO {_tableName} (CreatorUserId, AccountName, SubscriptionLevel, IsActive) " +
                   $"VALUES (@CreatorUserId, @AccountName, @SubscriptionLevel, @IsActive)";
        }

        protected override SqlParameter[] GetInsertParameters(Account entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@AccountName", entity.AccountName),
                new SqlParameter("@SubscriptionLevel", entity.SubscriptionLevel),
                new SqlParameter("@IsActive", entity.IsActive),
                new SqlParameter("@CreatorUserId", entity.CreatorUserId)
            };
        }

        protected override string BuildUpdateSql(Account entity)
        {
            // O UPDATE de uma conta deve ser cauteloso, por isso inclui apenas campos mutáveis.
            return $"UPDATE {_tableName} SET AccountName = @AccountName, SubscriptionLevel = @SubscriptionLevel, IsActive = @IsActive " +
                   $"WHERE AccountId = @AccountId";
        }

        protected override SqlParameter[] GetUpdateParameters(Account entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@AccountName", entity.AccountName),
                new SqlParameter("@SubscriptionLevel", entity.SubscriptionLevel),
                new SqlParameter("@IsActive", entity.IsActive),
                new SqlParameter("@AccountId", entity.AccountId)
            };
        }

        public async Task<Account?> GetByNameAsync(string accountName)
        {
            // O SELECT deve trazer todas as colunas necessárias para o MapFromReader
            string sql = $@" SELECT AccountId, CreatorUserId, AccountName, SubscriptionLevel, IsActive 
                             FROM {_tableName} WHERE AccountName = @AccountName AND IsActive = 1";

            SqlParameter param = new SqlParameter("@AccountName", accountName);

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, param))
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

        public async Task<IEnumerable<Account>> GetAccountsByUserIdAsync(int userId)
        {
            string sql = $@"
                SELECT A.AccountId, A.CreatorUserId, A.AccountName, A.SubscriptionLevel, A.IsActive
                FROM {_tableName} A
                INNER JOIN [Users] U ON A.AccountId = U.AccountId
                WHERE U.UserId = @UserId AND A.IsActive = 1";

            SqlParameter param = new SqlParameter("@UserId", userId);

            try
            {
                var accounts = new List<Account>();
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, param))
                {
                    while (reader.Read())
                    {
                        accounts.Add(MapFromReader(reader));
                    }
                }
                return accounts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetAccountsByUserIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Account>> GetUserActiveAccountsAsync(int userId)
        {
            string sql = $@"
                SELECT A.AccountId, A.CreatorUserId, A.AccountName, A.SubscriptionLevel, A.IsActive
                FROM {_tableName} A
                INNER JOIN [Users] U ON A.AccountId = U.AccountId
                WHERE U.UserId = @UserId AND A.IsActive = 1";

            SqlParameter param = new SqlParameter("@UserId", userId);

            try
            {
                var accounts = new List<Account>();
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, param))
                {
                    while (reader.Read())
                    {
                        accounts.Add(MapFromReader(reader));
                    }
                }
                return accounts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetUserActiveAccountsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AccountNameExistsAsync(string accountName, int? excludeId = null)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@AccountName", accountName)
            };

            string sql = $@"SELECT COUNT(1)
                             FROM {_tableName}
                             WHERE AccountName = @AccountName AND IsActive = 1";

            if (excludeId.HasValue)
            {
                // Se excludeId tiver um valor, adicionamos a condição de exclusão no WHERE
                sql += " AND AccountId != @ExcludeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            try
            {
                object? result = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());

                if (result != null && result != DBNull.Value && Convert.ToInt32(result) > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório AccountNameExistsAsync: {ex.Message}");
                throw;
            }

            return false;
        }
    }
}
