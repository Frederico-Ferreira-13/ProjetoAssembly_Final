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
        public string AccountName { get; protected set; } = string.Empty;
        public string SubscriptionLevel { get; protected set; } = "Free";
        public int? CreatorUserId { get; protected set; }
        public bool IsActive { get; private set; }
        public DateTime? LastUpdatedAt { get; private set; }        

        private readonly List<Users> _users = new();
        public virtual IReadOnlyCollection<Users> Users => _users.AsReadOnly();

        public Account(string accountName, string subscriptionLevel, int creatorUserId)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("O nome da conta é obrigatório.", nameof(accountName));
            }                

            if (accountName.Length > 255)
            {
                throw new ArgumentException("O nome da conta não pode exceder 255 caracteres.", nameof(accountName));
            }                

            if (!string.IsNullOrEmpty(subscriptionLevel) && subscriptionLevel.Length > 50)
            {
                throw new ArgumentException("O nível de subscrição não pode exceder 50 caracteres.", nameof(subscriptionLevel));
            }

            AccountName = accountName;
            SubscriptionLevel = string.IsNullOrWhiteSpace(subscriptionLevel) ? "Free" : subscriptionLevel;
            CreatorUserId = creatorUserId;
            IsActive = true;
            AccountId = default;            
        }

        protected Account(int id, string accountName, string subscriptionLevel, int? creatorUserId, bool isActive, DateTime? lastUpdatedAt = null)
        {
            AccountId = id;
            AccountName = accountName;
            SubscriptionLevel = subscriptionLevel;
            CreatorUserId = creatorUserId;
            IsActive = isActive;
            LastUpdatedAt = lastUpdatedAt;
        }

        public static Account Reconstitute(int id, string accountName, string subscriptionLevel, int? creatorUserId, bool isActive, DateTime? lastUpdatedAt = null)
        {
            return new Account(id, accountName, subscriptionLevel, creatorUserId, isActive, lastUpdatedAt);
        }

        public void UpdateDetails(string newAccountName, string newSubscriptionLevel)
        {
            if (string.IsNullOrWhiteSpace(newAccountName))
            {
                throw new ArgumentException("O nome da conta é obrigatório.", nameof(newAccountName));
            }                

            if (newAccountName.Length > 255)
            {
                throw new ArgumentException("O nome da conta não pode exceder 255 caracteres.", nameof(newAccountName));
            }                

            if (!string.IsNullOrEmpty(newSubscriptionLevel) && newSubscriptionLevel.Length > 50)
            {
                throw new ArgumentException("O nível de subscrição não pode exceder 50 caracteres.", nameof(newSubscriptionLevel));
            }               

            bool changed = false;

            if (!string.Equals(AccountName, newAccountName, StringComparison.OrdinalIgnoreCase))
            {
                AccountName = newAccountName;
                changed = true;
            }

            if (!string.Equals(SubscriptionLevel, newSubscriptionLevel, StringComparison.OrdinalIgnoreCase))
            {
                SubscriptionLevel = newSubscriptionLevel;
                changed = true;
            }

            if(changed)
            {
                LastUpdatedAt = DateTime.UtcNow;
            }
        }
        
        public void SetUsers(IEnumerable<Users> users)
        {
            _users.Clear();
            if(users != null)
            {
                foreach (var user in users)
                {
                    if (user != null)
                    {
                        _users.Add(user);
                    }                        
                }
            }            
        }

        public void AddUsers(Users user)
        {
            if(user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _users.Add(user);
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

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

        public void Activate()
        {
            if(!IsActive)
            {
                IsActive = true;
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

       
    }
}
