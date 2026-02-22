using Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class UserSettings : IEntity
    {
        public int UserSettingId { get; private set; }       

        public int UserId { get; protected set; }

        public string Theme { get; protected set; }
        public string Language { get; protected set; }
        public bool NotificationsEnabled { get; protected set; }

        [SetsRequiredMembers]
        private UserSettings()
        {
            UserSettingId = default;
            Theme = string.Empty;
            Language = string.Empty;
            NotificationsEnabled = false;
        }

        public UserSettings(int userId, string theme, string language, bool notificationsEnabled)
        {
            ValidateSettings(userId, theme, language);

            this.UserSettingId = default;           

            UserId = userId;
            Theme = theme;
            Language = language;
            NotificationsEnabled = notificationsEnabled;
        }

        private UserSettings(int id, int userId, string theme, string language,
            bool notificationsEnabled)
        {
            UserSettingId = id;            

            UserId = userId;
            Theme = theme;
            Language = language;
            NotificationsEnabled = notificationsEnabled;
        }

        public static UserSettings Reconstitute(int id, int userId, string theme, string language,
            bool notificationsEnabled)
        {
            return new UserSettings(id, userId, theme, language, notificationsEnabled);
        }

        public void UpdateSettings(string newTheme, string newLanguage, bool newNotificationsEnabled)
        {            

            ValidateSettings(UserId, newTheme, newLanguage);

            if (Theme != newTheme)
            {
                Theme = newTheme;
            }
            if (Language != newLanguage)
            {
                Language = newLanguage;
            }
            if (NotificationsEnabled != newNotificationsEnabled)
            {
                NotificationsEnabled = newNotificationsEnabled;
            }
        }

        private static void ValidateSettings(int userId, [NotNull] string? theme, [NotNull] string? language)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), "O ID do Utilizador deve ser positivo.");
            }
            if (string.IsNullOrWhiteSpace(theme))
            {
                throw new ArgumentException("O tema não pode ser vazio.", nameof(theme));
            }
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("O idioma não pode ser vazio.", nameof(language));
            }

            if (language.Length > 10)
            {
                throw new ArgumentException("O idioma não pode exceder 10 caracteres (ex: 'pt-PT')", nameof(language));
            }
        }

        public void UpdateTheme(string newTheme)
        {
            if (Theme != newTheme)
            {
                UpdateSettings(newTheme, Language, NotificationsEnabled);
            }
        }

        public void UpdateLanguage(string newLanguage)
        {
            if (Language != newLanguage)
            {
                UpdateSettings(Theme, newLanguage, NotificationsEnabled);
            }
        }

        public void UpdateReceiveNotifications(bool newNotificationsEnabled)
        {
            if (NotificationsEnabled != newNotificationsEnabled)
            {
                UpdateSettings(Theme, Language, newNotificationsEnabled);
            }
        }       

        public int GetId() => UserSettingId;

        public void SetId(int id)
        {
            if (UserSettingId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            UserSettingId = id;
        }

        public bool GetIsActive() => true;
    }
}
