using Core.Common;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Model
{
    public class Users : IEntity
    {
        public int UserId { get; private set; }
        public bool IsActive { get; private set; } = true;
        public int AccountId { get; protected set; }
        public int UsersRoleId { get; private set; }

        // Campos Privados
        private string _userName = string.Empty;
        private string _email = string.Empty;
        private string _passwordHash = string.Empty;
        private string _salt = string.Empty;

        // Propriedades de Dominio
        public bool IsApproved { get; private set; } = false;
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastUpdatedAt { get; private set; }

        // Propriedades de Navegação
        public Account? Account { get; protected set; }
        public UsersRole? Role { get; private set; }

        // public virtual UserSetting? UserSetting{get; internal set;} = null;
        public string UserName { get => _userName; private set => _userName = value; }
        public string Email { get => _email; private set => _email = value; }
        public string PasswordHash { get => _passwordHash; private set => _passwordHash = value; }
        public string Salt { get => _salt; private set => _salt = value; }

        private Users()
        {
            this.UserId = default;
            this.IsActive = true;
        }

        public Users(string userName, string email, string passwordHash, string salt, int usersRoleId,
             bool isApproved, int accountId)
        {
            ValidateUserCreation(userName, email, passwordHash, salt);
            if (accountId <= 0)
            {
                throw new ArgumentException("O Account deve ser positivo", nameof(accountId));
            }
            if (usersRoleId <= 0)
            {
                throw new ArgumentException("O Nível de Acesso (UsersRoleId) deve ser positivo", nameof(usersRoleId));
            }

            this.UserId = default;
            this.IsActive = true;

            UserName = userName;
            Email = email;
            PasswordHash = passwordHash;
            Salt = salt;
            UsersRoleId = usersRoleId;
            IsApproved = isApproved;
            AccountId = accountId;

            this.CreatedAt = DateTime.UtcNow;
            this.LastUpdatedAt = null;
        }

        protected Users(int id, bool isActive, string userName, string email, string passwordHash, string salt, bool isApproved, int usersRoleId, int accountId,
            DateTime createdAt, DateTime? lastUpdatedAt)
        {
            this.UserId = id;
            this.IsActive = isActive;

            _userName = userName;
            _email = email;
            _passwordHash = passwordHash;
            _salt = salt;
            UsersRoleId = usersRoleId;
            IsApproved = isApproved;
            AccountId = accountId;

            this.CreatedAt = createdAt;
            this.LastUpdatedAt = lastUpdatedAt;
        }

        public static Users Reconstitute(int id, string userName, string email, string passwordHash, string salt, bool isApproved, int usersRoleId, int accountId,
            DateTime createdAt, DateTime? lastUpdatedAt, bool isActive)
        {
            return new Users(
                id,
                isActive,
                userName,
                email,
                passwordHash,
                salt,
                isApproved,
                usersRoleId,
                accountId,
                createdAt,
                lastUpdatedAt
            );
        }

        private void SetLastUpdatedAt()
        {
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void ChangeRole(int newUsersRoleId)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar o nível de acesso de um utilizador inativo.");
            }
            if (newUsersRoleId <= 0)
            {
                throw new ArgumentException("O novo UsersRoleId deve ser positivo.", nameof(newUsersRoleId));
            }

            if (UsersRoleId != newUsersRoleId)
            {
                UsersRoleId = newUsersRoleId;
                SetLastUpdatedAt();
            }
        }

        public void Deactivate()
        {
            if (this.IsActive)
            {
                this.IsActive = false;
                SetLastUpdatedAt();
            }
        }

        public void Activate()
        {
            if (!this.IsActive)
            {
                this.IsActive = true;
                SetLastUpdatedAt();
            }
        }

        public void UpdateUserName(string newUserName)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar um utilizador inativo.");
            }

            ValidateName(newUserName, nameof(newUserName));

            if (!string.Equals(UserName, newUserName, StringComparison.OrdinalIgnoreCase))
            {
                UserName = newUserName;
                SetLastUpdatedAt();
            }
        }

        public void UpdateEmail(string newEmail)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar um utilizador inativo.");
            }

            ValidateEmail(newEmail);

            if (!string.Equals(Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                Email = newEmail;
                SetLastUpdatedAt();
            }
        }

        public void SetPassword(string newPasswordHash, string newSalt)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar a password de um utilizador inativo.");
            }

            ValidatePassword(newPasswordHash);
            ValidateSalt(newSalt);

            PasswordHash = newPasswordHash;
            Salt = newSalt;
            SetLastUpdatedAt();
        }

        public void ChangeAccount(int newAccountId)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível mudar a conta de um utilizador inativo.");
            }
            if (newAccountId <= 0)
            {
                throw new ArgumentException("O novo AccountId deve ser positivo.", nameof(newAccountId));
            }

            if (AccountId != newAccountId)
            {
                AccountId = newAccountId;
                Account = null;
                SetLastUpdatedAt();
            }
        }

        public void SetAccount(Account account)
        {
            if (AccountId != account.AccountId)
            {
                throw new InvalidOperationException("Erro de mapeamento: O ID da Account não coincide com o ID injetado.");
            }
            Account = account;
        }

        private void ValidateUserCreation(string userName, string email, string passwordHash, string salt)
        {
            ValidateName(userName, nameof(userName));
            ValidateEmail(email);
            ValidatePassword(passwordHash);
            ValidateSalt(salt);
        }

        private void ValidateName(string name, string paramName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"O nome de utilizador não pode ser vazio ou nulo ({paramName}).");
            }
            if (name.Length > 100)
            {
                throw new ArgumentException("O nome de utilizador não pode exceder 100 caracteres.", paramName);
            }
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("O email é obrigatório.", nameof(email));
            }
            if (email.Length > 255)
            {
                throw new ArgumentException("O email não pode exceder 255 caracteres.", nameof(email));
            }
            if (!IsValidEmailFormat(email))
            {
                throw new ArgumentException("Formato de email inválido.", nameof(email));
            }
        }

        private void ValidatePassword(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("O hash da password é obrigatório.", nameof(passwordHash));
            }
            if (passwordHash.Length < 60)
            {
                throw new ArgumentException("O hash da password tem um comprimento inválido.", nameof(passwordHash));
            }
        }

        private void ValidateSalt(string salt)
        {
            if (string.IsNullOrWhiteSpace(salt))
            {
                throw new ArgumentException("O salt da password é obrigatório.", nameof(salt));
            }
            if (salt.Length < 16 || salt.Length > 64)
            {
                throw new ArgumentException("O salt tem um cumprimento inválido.", nameof(salt));
            }
        }

        private bool IsValidEmailFormat(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        public void Approve()
        {
            if (this.IsApproved)
            {
                return;
            }

            if (!this.IsActive)
            {
                throw new InvalidOperationException("Não é poss´´ivel aprovar um utilizador inativo.");
            }

            this.IsApproved = true;
            SetLastUpdatedAt();
        }

        public int GetId() => UserId;

        public void SetId(int id)
        {
            if (UserId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            UserId = id;
        }

        public bool GetIsActive() => IsActive;
    }
}
