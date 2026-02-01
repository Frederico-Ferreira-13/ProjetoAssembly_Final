using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class UsersService : IUsersService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserSettingsService _userSettingsService;
        private readonly IAuthenticationService _authenticationService;

        public UsersService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IUserSettingsService userSettingsService,
            IAuthenticationService authenticationService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));            
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        public async Task<Result<Users>> RegisterUserAsync(Users newUser, string password)
        {
            if (await _unitOfWork.Users.GetUserByUserNameAsync(newUser.UserName) != null)
            {
                return Result<Users>.Failure(
                    Error.Validation("Nome de utilizador em uso."));
            }

            if (await _unitOfWork.Users.GetByEmailAsync(newUser.Email) != null)
            {
                return Result<Users>.Failure(
                    Error.Validation("Email em uso."));
            }            

            try
            {
                var salt = _passwordHasher.GenerateSalt();
                var hashResult = _passwordHasher.HashPassword(password, salt);

                if (!hashResult.IsSuccessful)
                {
                    return Result<Users>.Failure(hashResult.Error);
                }

                var roleId = (newUser.Email.ToLower() == "fredericocrf87@hotmail.com") ? 1 : 2;
                var aproved = (roleId == 1);

                var userToSave = new Users(
                    userName: newUser.UserName,
                    email: newUser.Email,
                    passwordHash: hashResult.Value.Hash,
                    salt: salt,
                    usersRoleId: roleId,
                    isApproved: aproved,
                    accountId: newUser.AccountId
                );

                await _unitOfWork.Users.CreateAddAsync(userToSave);
                await _unitOfWork.CommitAsync();

                if (userToSave.UserId > 0)
                {
                    await _userSettingsService.CreateDefaultSettingsAsync(userToSave.UserId);
                }
                return Result<Users>.Success(userToSave);
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Users>.Failure(
                    Error.Validation(
                    "Dados de registo inválidos.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }
        }

        public async Task<Result<Users>> AuthenticateUserAsync(string identifier, string password)
        {
            var authResult = await _authenticationService.AuthenticateAsync(identifier, password);

            if (!authResult.IsSuccessful)
            {
                return Result<Users>.Failure(authResult.Error);
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(authResult.Value.UserId);
                   

            if (user == null || !user.IsActive)
            {
                return Result<Users>.Failure(
                    Error.Validation(
                    "A conta está inativa.")
                );
            }

            return Result<Users>.Success(user);
        }

        public async Task<Result<Users>> GetUserByIdAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            return user == null
                ? Result<Users>.Failure(Error.NotFound(ErrorCodes.NotFound, "Não encontrado."))
                : Result<Users>.Success(user);
        }

        public async Task<Result<Users>> GetUserByEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
            {
                return Result<Users>.Failure(Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Utilizador com email '{email}' não encontrado.")
                );
            }
            return Result<Users>.Success(user);
        }

        public async Task<Result> UpdateUserProfileAsync(Users userToUpdate)
        {
            var existingUser = await _unitOfWork.Users.ReadByIdAsync(userToUpdate.UserId);
            if (existingUser == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound, "Não encontrado.")
                );
            }

            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userToUpdate.UserId)
            {
                return Result.Failure(Error.Forbidden(ErrorCodes.AuthForbidden, "Acesso negado."));
            }

            if (!string.IsNullOrWhiteSpace(userToUpdate.UserName))
            {
                existingUser.UpdateUserName(userToUpdate.UserName);
            }

            if (!string.IsNullOrWhiteSpace(userToUpdate.Email))
            {
                existingUser.UpdateEmail(userToUpdate.Email);
            }          

            await _unitOfWork.Users.UpdateAsync(existingUser);
            await _unitOfWork.CommitAsync();

            return Result.Success("Atualizado.");
        }

        public async Task<Result> ChangeUserPasswordAsync(int userId, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Result.Failure(Error.Validation(
                    "A password antiga e a nova password não podem ser vazias.",
                    new Dictionary<string, string[]> { { "Password", new[] { "Passwords não podem ser nulas." } } })
                );
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    "Não foi possível alterar a password. Utilizador não encontrado ou inativo.")
                );
            }

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, oldPassword, user.Salt))
            {
                return Result.Failure(
                    Error.Validation(
                    "Password antiga incorreta.")
                );
            }

            try
            {
                var newSalt = _passwordHasher.GenerateSalt();
                var hashResult = _passwordHasher.HashPassword(newPassword, newSalt);

                if (!hashResult.IsSuccessful)
                {
                    return Result.Failure(hashResult.Error);
                }

                var newPasswordHash = hashResult.Value.Hash;
                var newUsedSalt = hashResult.Value.Salt;

                user.SetPassword(newPasswordHash, newSalt);

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.CommitAsync();

                return Result.Success("Password alterada com sucesso.");
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(
                    Error.Validation(
                    "A nova password não cumpre os requisitos.",
                    new Dictionary<string, string[]> { { "newPassword", new[] { ex.Message } } })
                );
            }
        }

        public async Task<Result> DeactivateUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user != null)
            {
                user.Deactivate();
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.CommitAsync();
            }            

            return Result.Success();
        }

        public async Task<Result> ActivateUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound, $"Utilizador com ID {userId} não encontrado.")
                );
            }

            user.Activate();
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            return Result.Success($"Utilizador {userId} ativado com sucesso.");
        }

        public async Task<Result> DeleteUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null)
            {
                return Result.Success("Utilizador não encontrado ou já eliminado.");
            }
            await _unitOfWork.Users.RemoveAsync(user);
            await _unitOfWork.CommitAsync();

            return Result.Success("Utilizador eliminado com sucesso.");
        }

        public async Task<Result<Users>> GetUserByUsernameOrEmailAsync(string identifier)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(identifier);

            if (user == null)
            {
                user = await _unitOfWork.Users.GetUserByUserNameAsync(identifier);
            }

            if (user == null)
            {
                return Result<Users>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Utilizador com identificador '{identifier}' não encontrado.")
                );
            }

            return Result<Users>.Success(user);
        }

        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            return await _unitOfWork.Users.GetByEmailAsync(email) != null;
        }

        public async Task<bool> IsUserNameUniqueAsync(string userName)
        {
            return await _unitOfWork.Users.GetUserByUserNameAsync(userName) == null;
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            return await _unitOfWork.Users.ReadByIdAsync(userId) != null;
        }

        public async Task<Result<int>> GetCurrentUserIdAsync()
        {
            return await _authenticationService.GetCurrentUserIdAsync();
        }

        public async Task<Result<Users>> GetCurrentUserAsync()
        {
            var userResult = await _authenticationService.GetPersistedUserAsync();

            if (!userResult.IsSuccessful)
            {
                return Result<Users>.Failure(userResult.Error);
            }

            var users = userResult.Value;

            if (users == null)
            {
                return Result<Users>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "Utilizador autenticado não encontrado ou expirado.")
                );
            }

            return Result<Users>.Success(users);
        }
    }
}
