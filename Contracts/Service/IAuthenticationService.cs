using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IAuthenticationService
    {
        Task<Result<int>> GetCurrentUserIdAsync();
        Task<Result<Users>> AuthenticateAsync(string identifier, string password);
        Task LogoutAsync();
        Task<Result<Users>> GetPersistedUserAsync();
    }
}
