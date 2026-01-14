using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        

        public AuthenticationService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));            
        }

        public async Task<Result<int>> GetCurrentUserIdAsync()
        {
            return await _tokenService.GetUserIdFromContextAsync();
        }

        public async Task<Result<Users>> AuthenticateAsync(string identifier, string password)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(identifier);
            if (user == null)
            {
                user = await _unitOfWork.Users.GetUserByUserNameAsync(identifier);
            }

            if (user == null || !user.IsActive)
            {
                return Result<Users>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthFailed,
                    "Credenciais inválidas.")
                );
            }

            if (!user.IsActive)
            {
                return Result<Users>.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthLockedOut,
                    "A sua conta encontra-se inativa ou bloqueada.")
                );
            }

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, password, user.Salt))
            {
                return Result<Users>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthFailed,
                    "Credenciais inválidas.")
                );
            }
            
            return Result<Users>.Success(user);
        }

        public async Task LogoutAsync()
        {
            await _tokenService.InvalidateTokenAsync();
        }

        public async Task<Result<Users>> GetPersistedUserAsync()
        {
            var userIdResult = await GetCurrentUserIdAsync();

            if (!userIdResult.IsSuccessful)
            {
                return Result<Users>.Failure(
                    Error.Unauthorized(
                    userIdResult.ErrorCode ?? ErrorCodes.AuthUnauthorized,
                    userIdResult.Message ?? "Não autenticado ou token inválido.")
                );
            }

            int userId = userIdResult.Value;

            if (userId <= 0)
            {
                return Result<Users>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "Não autenticado ou token inválido.")
                );
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(userId);

            if (user == null)
            {
                return Result<Users>.Failure(
                    Error.NotFound(
                     ErrorCodes.NotFound,
                     "O utilizador associado ao token não existe.")
                 );
            }

            if (!user.IsActive)
            {
                return Result<Users>.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "A sua conta encontra-se inativa ou bloqueada.")
                );
            }
            
            return Result<Users>.Success(user);
        }
    }
}
