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
        public UserSettingsRepository() : base("UserSettings") { }

        protected override string PrimaryKeyName => "UserSettingId";

        protected override UserSettings MapFromReader(SqlDataReader reader)
        {
            return new UserSettings(
                id: reader.GetInt32(reader.GetOrdinal("UserSettingId")),
                userId: reader.GetInt32(reader.GetOrdinal("UserId")),
                theme: reader.GetString(reader.GetOrdinal("Theme")),
                language: reader.GetString(reader.GetOrdinal("Language")),
                receiveNotifications: reader.GetBoolean(reader.GetOrdinal("ReceiveNotifications"))
            );
        }

        protected override string BuildInsertSql(UserSettings entity)
        {
            return $@"INSERT INTO {_tableName} (UserId, Theme, Language, ReceiveNotifications)
                      VALUES (@UserId, @Theme, @Language, @ReceiveNotifications)";
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
            return $@"UPDATE {_tableName} 
              SET Theme = @Theme, 
                  Language = @Language,
                  ReceiveNotifications = @ReceiveNotifications                         
              WHERE UserSettingId = @UserSettingId";
        }

        protected override SqlParameter[] GetUpdateParameters(UserSettings entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@Theme", entity.Theme),
                new SqlParameter("@Language", entity.Language),
                new SqlParameter("@ReceiveNotifications", entity.ReceiveNotifications),
                new SqlParameter("@UserSettingId", entity.GetId())
            };
        }

        public async Task<UserSettings?> GetByUserId(int userId)
        {
            string sql = $@"SELECT UserSettingId, UserId, Theme, Language, ReceiveNotifications
                            FROM {_tableName}
                            WHERE UserId = @UserId";

            var parameters = new SqlParameter[] { new SqlParameter("@UserId", userId) };

            return await ExecuteSingleAsync(sql, parameters);
        }

        public async Task<IEnumerable<UserSettings>> GetByLanguageAsync(string language)
        {
            string sql = $@"SELECT UserSettingId, UserId, Theme, Language, ReceiveNotifications
                            FROM {_tableName}
                            WHERE Language = @Language";

            var parameters = new SqlParameter[] { new SqlParameter("@Language", language) };

            return await ExecuteListAsync(sql, parameters);
        }
    }
}
