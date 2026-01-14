using Core.Common;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Account : IEntity
    {
        public int AccountId { get; private set; }
        public bool IsActive { get; private set; }

        public int CreatorUserId { get; protected set; }

        public string AccountName { get; protected set; }
        public string SubscriptionLevel { get; protected set; } = "Free";

        private readonly List<Users> _users = new();
        public virtual IReadOnlyCollection<Users> Users => _users.AsReadOnly();

        public Account(string accountName, string subscriptionLevel, int userId)
        {
            ValidateAccountParameters(accountName);

            if (userId <= 0)
            {
                throw new ArgumentException("O ID do Utilizador Criador deve ser positivo.", nameof(userId));
            }

            IsActive = true;
            AccountName = accountName;
            SubscriptionLevel = subscriptionLevel;
            CreatorUserId = userId;
        }

        private Account(int id, bool isActive, string accountName, string subscriptionLevel, int creatorUserId)
        {
            AccountId = id;
            IsActive = isActive;

            AccountName = accountName;
            SubscriptionLevel = subscriptionLevel;
            CreatorUserId = creatorUserId;
        }

        public static Account Reconstitute(int id, bool isActive, string accountName, string subscriptionLevel, int creatorUserId)
        {
            return new Account(id, isActive, accountName, subscriptionLevel, creatorUserId);
        }

        public void Deactive()
        {
            if (IsActive)
            {
                IsActive = false;
            }
        }

        public void UpdateDetails(string newAccountName, string newSubscriptionLevel)
        {
            ValidateAccountParameters(newAccountName);

            bool changed = false;

            if (!string.Equals(AccountName, newAccountName, StringComparison.OrdinalIgnoreCase))
            {
                AccountName = newAccountName;
            }

            if (!string.Equals(SubscriptionLevel, newSubscriptionLevel, StringComparison.OrdinalIgnoreCase))
            {
                SubscriptionLevel = newSubscriptionLevel;
            }
        }

        private void ValidateAccountParameters(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("O nome da conta é obrigatório.", nameof(accountName));
            }

            if (accountName.Length > 255)
            {
                throw new ArgumentException("O nome da conta não pode exceder 255 caracteres.", nameof(accountName));
            }
        }

        public void SetUsers(IEnumerable<Users> users)
        {
            _users.Clear();
            _users.AddRange(users);
        }

        public int GetId() => AccountId;

        public void SetId(int id)
        {
            if (AccountId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            AccountId = id;
        }

        public bool GetIsActive() => IsActive;
    }
}
