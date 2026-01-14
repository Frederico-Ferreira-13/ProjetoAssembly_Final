using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface ITokenService
    {
        TokenResponse GenerateToken(Users user);
        Task<Result<int>> GetUserIdFromContextAsync();
        Task InvalidateTokenAsync();
    }
}
