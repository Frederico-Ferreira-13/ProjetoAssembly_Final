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
        public bool IsActive { get; private set; } = true;

        public int UserId { get; protected set; }

        public string Theme { get; protected set; }
        public string Language { get; protected set; }
        public bool ReceiveNotifications { get; protected set; }

        [SetsRequiredMembers]
        private UserSettings()
        {
            this.UserSettingId = default;
            Theme = string.Empty;
            Language = string.Empty;
            ReceiveNotifications = false;
        }

        public UserSettings(int userId, string theme, string language, bool receiveNotifications)
        {
            ValidateSettings(userId, theme, language);

            this.UserSettingId = default;
            this.IsActive = true;

            UserId = userId;
            Theme = theme;
            Language = language;
            ReceiveNotifications = receiveNotifications;
        }

        private UserSettings(int id, bool isActive, int userId, string theme, string language,
            bool receiveNotifications)
        {
            this.UserSettingId = id;
            this.IsActive = isActive;

            UserId = userId;
            Theme = theme;
            Language = language;
            ReceiveNotifications = receiveNotifications;
        }

        public static UserSettings Reconstitute(int id, bool isActive, int userId, string theme, string language,
            bool receiveNotifications)
        {
            return new UserSettings(id, isActive, userId, theme, language, receiveNotifications);
        }

        public void UpdateSettings(string newTheme, string newLanguage, bool newReceiveNotifications)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar configurações");
            }

            ValidateSettings(UserId, newTheme, newLanguage);

            if (Theme != newTheme)
            {
                Theme = newTheme;
            }
            if (Language != newLanguage)
            {
                Language = newLanguage;
            }
            if (ReceiveNotifications != newReceiveNotifications)
            {
                ReceiveNotifications = newReceiveNotifications;
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
                UpdateSettings(newTheme, Language, ReceiveNotifications);
            }
        }

        public void UpdateLanguage(string newLanguage)
        {
            if (Language != newLanguage)
            {
                UpdateSettings(Theme, newLanguage, ReceiveNotifications);
            }
        }

        public void UpdateReceiveNotifications(bool newReceiveNotifications)
        {
            if (ReceiveNotifications != newReceiveNotifications)
            {
                UpdateSettings(Theme, Language, newReceiveNotifications);
            }
        }

        public void Deactivate()
        {
            if (this.IsActive)
            {
                this.IsActive = false;
            }
        }
        public void Activate()
        {
            if (!this.IsActive)
            {
                this.IsActive = true;
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

        public bool GetIsActive() => IsActive;
    }
}
