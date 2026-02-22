using Core.Common;
using System;
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
        private string _name = string.Empty;
        private string _userName = string.Empty;
        private string _email = string.Empty;
        private string? _profilePicture;
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
        public string Name { get => _name; private set => _name = value;  }
        public string UserName { get => _userName; private set => _userName = value; }
        public string Email { get => _email; private set => _email = value; }
        public string PasswordHash { get => _passwordHash; private set => _passwordHash = value; }
        public string Salt { get => _salt; private set => _salt = value; }
        public string? ProfilePicture { get => _profilePicture; private set => _profilePicture = value; }

        private Users()
        {
            // Futuramente para EF core ou para Reconstituição via reflection
        }

        public Users(string name, string userName, string email, int usersRoleId, bool isApproved, int accountId)
        {
            ValidateName(name, nameof(name));
            ValidateUserName(userName, nameof(userName));
            ValidateEmail(email);

            if (accountId <= 0)
            {
                throw new ArgumentException("O Account deve ser positivo", nameof(accountId));
            }
            if (usersRoleId <= 0)
            {
                throw new ArgumentException("O Nível de Acesso (UsersRoleId) deve ser positivo", nameof(usersRoleId));
            }

            _name = name;
            _userName = userName;
            _email = email;
            UsersRoleId = usersRoleId;
            IsApproved = isApproved;
            AccountId = accountId;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        protected Users(int id, bool isActive, string name, string userName, string email, string passwordHash, string salt, bool isApproved, int usersRoleId, int accountId,
            DateTime createdAt, DateTime? lastUpdatedAt)
        {
            UserId = id;
            IsActive = isActive;
            _name = name;
            _userName = userName;
            _email = email;
            _passwordHash = passwordHash;
            _salt = salt;
            IsApproved = isApproved;
            UsersRoleId = usersRoleId;
            AccountId = accountId;
            CreatedAt = createdAt;
            LastUpdatedAt = lastUpdatedAt;            
        }

        public static Users Reconstitute(int id, string name, string userName, string email, string passwordHash, string salt, bool isApproved, int usersRoleId, int accountId,
            DateTime createdAt, DateTime? lastUpdatedAt, bool isActive)
        {
            return new Users(
                id,                
                isActive,
                name,
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

        public void UpdateName(string newName)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Utilizador inativo.");
            } 

            ValidateName(newName, nameof(newName));

            if (_name != newName)
            {
                _name = newName;
                SetLastUpdatedAt();
            }
        }

        public void UpdateUserName(string newUserName)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Utilizador inativo.");
            }
            
            ValidateUserName(newUserName, nameof(newUserName));

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
                throw new InvalidOperationException("Utilizador inativo.");
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

            if(string.IsNullOrWhiteSpace(newPasswordHash) || string.IsNullOrWhiteSpace(newSalt))
            {
                throw new ArgumentException("O hash da password e o salt são obrigatórios.");
            }

            PasswordHash = newPasswordHash;
            Salt = newSalt;
            SetLastUpdatedAt();
        }

        public void UpdateProfilePicture(string? fileName)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar a foto de um utilizador inativo.");
            }

            if (_profilePicture != fileName)
            {
                _profilePicture = fileName;
                SetLastUpdatedAt();
            }
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

            IsApproved = true;
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

        private void ValidateUserName(string userName, string paramName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException($"O nome de utilizador não pode ser vazio ou nulo ({paramName}).");

            if (userName.Length > 100)
                throw new ArgumentException("O nome de utilizador não pode exceder 100 caracteres.", paramName);
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

        private bool IsValidEmailFormat(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        private void SetLastUpdatedAt()
        {
            LastUpdatedAt = DateTime.UtcNow;
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
