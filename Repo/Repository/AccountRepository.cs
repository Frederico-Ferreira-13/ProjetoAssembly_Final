using Core.Model;
using Contracts.Repository;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using Core.Common;

namespace Repo.Repository
{
    public class AccountRepository : SoftDeleteRepository<Account>, IAccountRepository
    {
        public AccountRepository() : base("Account") { }
        protected override string PrimaryKeyName => "AccountId";        

        protected override Account MapFromReader(SqlDataReader reader)
        {
            return new Account(
                id: reader.GetInt32(reader.GetOrdinal("AccountId")),
                accountName: reader.GetString(reader.GetOrdinal("AccountName")),
                subscriptionLevel: reader.GetString(reader.GetOrdinal("SubscriptionLevel")),
                creatorUserId: reader.IsDBNull(reader.GetOrdinal("CreatorUserId")) ? null : reader.GetInt32(reader.GetOrdinal("CreatorUserId")),
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive"))
            );
        }

        protected override string BuildInsertSql(Account entity)
        {
            // Insere apenas os campos necessários (ID é IDENTITY)
            return $@"INSERT INTO {_tableName} (AccountName, SubscriptionLevel, IsActive, CreatorUserId)
                   VALUES (@AccountName, @SubscriptionLevel, @IsActive, @CreatorUserId)";
        }

        protected override SqlParameter[] GetInsertParameters(Account entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@AccountName", entity.AccountName ?? (object)DBNull.Value),
                new SqlParameter("@SubscriptionLevel", entity.SubscriptionLevel ?? (object)DBNull.Value),
                new SqlParameter("@IsActive", entity.IsActive),
                new SqlParameter("@CreatorUserId", entity.CreatorUserId ?? (object)DBNull.Value)
            };
        }

        protected override string BuildUpdateSql(Account entity)
        {
            // O UPDATE de uma conta deve ser cauteloso, por isso inclui apenas campos mutáveis.
            return $@"UPDATE {_tableName}
                      SET AccountName = @AccountName,
                        SubscriptionLevel = @SubscriptionLevel,
                        IsActive = @IsActive,
                        CreatorUserId = @CreatorUserId
                   WHERE AccountId = @AccountId";
        }

        protected override SqlParameter[] GetUpdateParameters(Account entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@AccountName", entity.AccountName ?? (object)DBNull.Value),
                new SqlParameter("@SubscriptionLevel", entity.SubscriptionLevel ?? (object)DBNull.Value),
                new SqlParameter("@IsActive", entity.IsActive),
                new SqlParameter("@CreatorUserId", entity.CreatorUserId ?? (object)DBNull.Value),
                new SqlParameter("@AccountId", entity.GetId())
            };
        }

        public async Task<Account?> GetByNameAsync(string accountName)
        {
            // O SELECT deve trazer todas as colunas necessárias para o MapFromReader
            string sql = $@" SELECT AccountId, AccountName, SubscriptionLevel, CreatorUserId, IsActive 
                             FROM {_tableName} 
                             WHERE AccountName = @AccountName AND IsActive = 1";

            var parameters = new SqlParameter[] { new SqlParameter("@AccountName", accountName) };

            return await ExecuteSingleAsync(sql, parameters);
        }

        public async Task<IEnumerable<Account>> GetAccountsByUserIdAsync(int userId)
        {
            string sql = $@"SELECT A.AccountId, A.AccountName, A.CreatorUserId, A.SubscriptionLevel, A.IsActive
                            FROM {_tableName} A
                            INNER JOIN Users U ON A.AccountId = U.AccountId
                            WHERE U.UserId = @UserId AND A.IsActive = 1
                            ORDER BY A.AccountName";

            var parameters = new SqlParameter[] { new SqlParameter("@UserId", userId) };

            return await ExecuteListAsync(sql, parameters);
        }

        public async Task<IEnumerable<Account>> GetUserActiveAccountsAsync(int userId)
        {
            return await GetAccountsByUserIdAsync(userId);
        }

        public async Task<bool> AccountNameExistsAsync(string accountName, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                return false;
            }

            string sql = $@"SELECT COUNT(1)
                             FROM {_tableName}
                             WHERE AccountName = @AccountName AND IsActive = 1";

            var parameters = new List<SqlParameter> { new SqlParameter("@AccountName", accountName) };

            if (excludeId.HasValue)
            {
                sql += " AND AccountId != @ExlcudeId";
                parameters.Add(new SqlParameter("@ExcludeId", excludeId.Value));
            }

            var count = await SQL.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(count) > 0;
        }
    }
}
