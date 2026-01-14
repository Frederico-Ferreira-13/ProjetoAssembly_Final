using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IUsersRepository : IRepository<Users>
    {
        Task<bool> ExistsByIdAsync(int id);
        Task<Users?> GetByEmailAsync(string email);
        Task<Users?> GetUserByUserNameAsync(string userName);
        Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
        Task<bool> ExistsByUserNameAsync(string userName, int? excludeId = null);
        Task<List<Users>> GetByApprovalStatusAsync(bool isApproved);
    }
}
