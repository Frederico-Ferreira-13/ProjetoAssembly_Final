using Contracts.Service;
using Core.Common;
using System;
using System.Collections.Generic;
using System.Text;
using BCrypt.Net;

namespace Service.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;

        public string GenerateSalt()
        {
            return BCrypt.Net.BCrypt.GenerateSalt(WorkFactor);
        }

        public Result<HashResult> HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
            {
                return Result<HashResult>.Failure(
                Error.Validation("Password não pode ser nula ou vazia."));
            }

            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
                var result = new HashResult(hashedPassword, salt);
                return Result<HashResult>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<HashResult>.Failure(
                    Error.InternalServer("Erro ao gerar hash da password: " + ex.Message));
            }
        }

        public bool VerifyPassword(string storedHash, string passwordToVerify, string salt)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(passwordToVerify))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(passwordToVerify, storedHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }
    }
}
