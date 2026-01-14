using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IAccountService
    {
        Task<Result<Account>> CreateAccountAsync(Account account);
        Task<Result<Account>> GetAccountByIdAsync(int id);
        Task<Result<IEnumerable<Account>>> GetAccountsByUserIdAsync(int userId);
        Task<Result<IEnumerable<Account>>> GetUserActiveAccountsAsync(int userId);
        Task<Result<IEnumerable<Account>>> GetCurrentUserAccountsAsync();
        Task<Result<Account>> UpdateAccountAsync(Account dto);
        Task<Result<Account>> DesactivateAccountAsync(int accountId);
    }
}
