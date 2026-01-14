using Core.Model;
using Core.Common;

namespace Contracts.Repository
{
    public interface IAccountRepository : IRepository<Account>
    {
        Task<Account?> GetByNameAsync(string accountName);
        Task<IEnumerable<Account>> GetAccountsByUserIdAsync(int userId);
        Task<IEnumerable<Account>> GetUserActiveAccountsAsync(int userId);

        Task<bool> AccountNameExistsAsync(string accountName, int? excludeId = null);
    }
}
