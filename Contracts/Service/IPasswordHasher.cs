using Core.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IPasswordHasher
    {
        string GenerateSalt();
        Result<HashResult> HashPassword(string password, string salt);
        bool VerifyPassword(string storedHash, string passwordToVerify, string salt);
    }
}
