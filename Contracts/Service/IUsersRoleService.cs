using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IUsersRoleService
    {
        Task<Result<UsersRole>> CreateUsersRoleAsync(UsersRole userRole);
        Task<Result<UsersRole>> GetUsersRoleByIdAsync(int id);
        Task<Result<IEnumerable<UsersRole>>> GetAllUsersRolesAsync();
        Task<Result> UpdateUsersRoleAsync(UsersRole updateUserRole);
        Task<Result> DeleteUsersRoleAsync(int id);
        Task<Result<UsersRole>> GetUsersRoleByNameAsync(string name);
    }
}
