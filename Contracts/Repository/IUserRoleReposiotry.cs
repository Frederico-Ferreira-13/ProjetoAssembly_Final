using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IUserRoleReposiotry : IRepository<UsersRole>
    {
        Task<UsersRole?> GetByNameAsync(string roleName);
    }
}
