using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class UserSettingsRepository : Repository<UserSettings>, IUserSettingsRepository
    {
        public UserSettingsRepository() : base("UserSettings")
        {
        }

        protected override UserSettings MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("UserSettingId"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
            string theme = reader.GetString(reader.GetOrdinal("Theme"));
            string language = reader.GetString(reader.GetOrdinal("Language"));
            bool receiveNotifications = reader.GetBoolean(reader.GetOrdinal("ReceiveNotifications"));

            return UserSettings.Reconstitute(
                id,
                isActive,
                userId,
                theme,
                language,
                receiveNotifications
            );
        }

        protected override string BuildInsertSql(UserSettings entity)
        {
            return $"INSERT INTO {_tableName} (UserId, Theme, Language, ReceiveNotifications, CreatedAt, IsActive) " +
                $"VALUES (@UserId, @Theme, @Language, @ReceiveNotifications, GETDATE(), 1)";
        }

        protected override SqlParameter[] GetInsertParameters(UserSettings entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@Theme", entity.Theme),
                new SqlParameter("@Language", entity.Language),
                new SqlParameter("@ReceiveNotifications", entity.ReceiveNotifications)
            };
        }

        protected override string BuildUpdateSql(UserSettings entity)
        {
            return $"UPDATE {_tableName} SET Theme = @Theme, Language = @Language, ReceiveNotifications = @ReceiveNotifications, LastUpdatedAt = GETDATE() " +
                $"WHERE UserSettingId = @Id";
        }

        protected override SqlParameter[] GetUpdateParameters(UserSettings entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@Theme", entity.Theme),
                new SqlParameter("@Language", entity.Language),
                new SqlParameter("@ReceiveNotifications", entity.ReceiveNotifications),
                new SqlParameter("@Id", entity.GetId())
            };
        }

        public async Task<UserSettings?> GetByUserId(int userId)
        {
            string sql = $"SELECT * FROM {_tableName} WHERE UserId = @UserId AND IsActive = 1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId)
            };

            return await ExecuteSingleAsync(sql, parameters);
        }

        public async Task<IEnumerable<UserSettings>> GetByLanguageAsync(string language)
        {
            string sql = $"SELECT * FROM {_tableName} WHERE Language = @Language AND IsActive = 1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Language", language)
            };
            return await ExecuteListAsync(sql, parameters);
        }
    }
}
