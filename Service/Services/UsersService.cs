using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services
{
    public class UsersService : IUsersService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;        
        private readonly IAuthenticationService _authenticationService;

        public UsersService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IAuthenticationService authenticationService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));                       
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        public async Task<Result<Users>> RegisterUserAsync(Users newUser, string password)
        {
            if (await _unitOfWork.Users.GetUserByUserNameAsync(newUser.UserName) != null)
            {
                return Result<Users>.Failure(
                    Error.Validation("Este nome de utilizador já está em uso. Escolha outro."));
            }

            if (await _unitOfWork.Users.GetByEmailAsync(newUser.Email) != null)
            {
                return Result<Users>.Failure(
                    Error.Validation("Este eail já está registado. Faça login ou use outro."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var salt = _passwordHasher.GenerateSalt();
                var hashResult = _passwordHasher.HashPassword(password, salt);
                if (!hashResult.IsSuccessful)
                {
                    _unitOfWork.Rollback();
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
                    await CreateDefaultSettingsAsync(userToSave.UserId);
                }

                return Result<Users>.Success(userToSave);
            }           
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                if(ex.Message.Contains("Violation of UNIQUE KEY") ||
                    ex.Message.Contains("Cannot insert duplicate key") ||
                    ex.Message.Contains("duplicate key value"))
                {
                    return Result<Users>.Failure(
                        Error.Validation("Este email ou nome de utilizador já está em uso. Tente outro."));
                }

                return Result<Users>.Failure(
                    Error.InternalServer("Erro ao criar conta. Tente novamente mais tarde."));
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
                    Error.Validation("A conta está inativa."));
            }

            return Result<Users>.Success(user);
        }

        public async Task<Result<Users>> GetUserByIdAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            return user == null
                ? Result<Users>.Failure(Error.NotFound(ErrorCodes.NotFound, "Utilizador não encontrado."))
                : Result<Users>.Success(user);
        }

        public async Task<Result<Users>> GetUserByEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
            {
                return Result<Users>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Utilizador com email '{email}' não encontrado."));
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
                        ErrorCodes.NotFound, 
                        "Utilizador não encontrado."));
            }

            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userToUpdate.UserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden, 
                        "Acesso negado."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
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

                return Result.Success("Perfil atualizado.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar perfil: {ex.Message}"));
            }
        }

        public async Task<Result> ChangeUserPasswordAsync(int userId, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Result.Failure(
                    Error.Validation(
                        "A password antiga e a nova password não podem ser vazias.",
                        new Dictionary<string, string[]> { { "Password", new[] { "Passwords não podem ser nulas." } } }));
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        "Não foi possível alterar a password. Utilizador não encontrado ou inativo."));
            }

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, oldPassword, user.Salt))
            {
                return Result.Failure(
                    Error.Validation("Password antiga incorreta."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newSalt = _passwordHasher.GenerateSalt();
                var hashResult = _passwordHasher.HashPassword(newPassword, newSalt);
                if (!hashResult.IsSuccessful)
                {
                    _unitOfWork.Rollback();
                    return Result.Failure(hashResult.Error);
                }

                user.SetPassword(hashResult.Value.Hash, newSalt);
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.CommitAsync();

                return Result.Success("Password alterada com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.Validation(
                        "A nova password não cumpre os requisitos.",
                        new Dictionary<string, string[]> { { "newPassword", new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao alterar password: {ex.Message}"));
            }
        }

        public async Task<Result> DeactivateUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null)
            {
                return Result.Success("Utilizador não encontrado.");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                user.Deactivate();
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.CommitAsync();

                return Result.Success("Utilizador desativado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao desativar utilizador: {ex.Message}"));
            }
        }

        public async Task<Result> ActivateUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound, 
                        $"Utilizador com ID {userId} não encontrado."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                user.Activate();
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.CommitAsync();

                return Result.Success($"Utilizador {userId} ativado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao ativar utilizador: {ex.Message}"));
            }
        }

        public async Task<Result> DeleteUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null)
            {
                return Result.Success("Utilizador não encontrado ou já eliminado.");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.Users.RemoveAsync(user);
                await _unitOfWork.CommitAsync();

                return Result.Success("Utilizador eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar utilizador: {ex.Message}"));
            }
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
                        $"Utilizador com identificador '{identifier}' não encontrado."));
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
                        "Utilizador autenticado não encontrado ou expirado."));
            }

            return Result<Users>.Success(users);
        }

        public async Task<Result<UserSettings>> GetSettingsByUserIdAsync(int userId)
        {
            var settings = await _unitOfWork.UserSettings.GetByUserId(userId);
            if (settings == null)
            {
                return Result<UserSettings>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Configurações para o utilizador com ID {userId} não foram encontradas."));
            }
            return Result<UserSettings>.Success(settings);
        }

        public async Task<Result> UpdateUserSettingsAsync(UserSettings settings)
        {
            if (settings == null || settings.UserId <= 0)
            {
                return Result.Failure(
                    Error.Validation(
                        "O ID do utilizador é obrigatório para atualizar as configurações."));
            }

            var existingSettings = await _unitOfWork.UserSettings.GetByUserId(settings.UserId);
            if (existingSettings == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Configurações para o utilizador com ID {settings.UserId} não encontradas. Crie as configurações padrão primeiro."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingSettings.UpdateSettings(
                    settings.Theme,
                    settings.Language,
                    settings.ReceiveNotifications
                );

                await _unitOfWork.UserSettings.UpdateAsync(existingSettings);
                await _unitOfWork.CommitAsync();

                return Result.Success("Configurações atualizadas com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para atualizar as configurações.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar configurações: {ex.Message}"));
            }
        }

        public async Task<Result<UserSettings>> CreateDefaultSettingsAsync(int userId)
        {
            var userExists = await _unitOfWork.Users.ReadByIdAsync(userId) != null;
            if (!userExists)
            {
                return Result<UserSettings>.Failure(
                    Error.BusinessRuleViolation(
                        ErrorCodes.BizInvalidOperation,
                        $"Utilizador com ID {userId} não existe. Não é possível criar configurações."));
            }

            var existingSettings = await _unitOfWork.UserSettings.GetByUserId(userId);
            if (existingSettings != null)
            {
                return Result<UserSettings>.Failure(
                    Error.Validation($"Configurações para o utilizador com ID {userId} já existem."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var defaultSettings = new UserSettings(
                    userId: userId,
                    theme: "Light",
                    language: "pt-PT",
                    receiveNotifications: true
                );

                await _unitOfWork.UserSettings.CreateAddAsync(defaultSettings);
                await _unitOfWork.CommitAsync();

                return Result<UserSettings>.Success(defaultSettings);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<UserSettings>.Failure(
                    Error.InternalServer($"Erro ao criar configurações padrão: {ex.Message}"));
            }
        }

        public async Task<Result<UsersRole>> CreateUsersRoleAsync(UsersRole dto)
        {
            if (await _unitOfWork.UsersRole.GetByNameAsync(dto.RoleName) != null)
            {
                return Result<UsersRole>.Failure(
                    Error.Validation(
                        $"O Nível de Acesso '{dto.RoleName}' já existe.",
                        new Dictionary<string, string[]> { { nameof(dto.RoleName), new[] { "Nome já em uso." } } }));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newRole = new UsersRole(dto.RoleName);
                await _unitOfWork.UsersRole.CreateAddAsync(newRole);
                await _unitOfWork.CommitAsync();

                return Result<UsersRole>.Success(newRole);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<UsersRole>.Failure(
                    Error.InternalServer($"Erro ao criar nível de acesso: {ex.Message}"));
            }
        }

        public async Task<Result<UsersRole>> GetUsersRoleByIdAsync(int id)
        {
            var role = await _unitOfWork.UsersRole.ReadByIdAsync(id);
            if (role == null)
            {
                return Result<UsersRole>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Nível de Acesso com ID {id} não encontrado."));
            }
            return Result<UsersRole>.Success(role);
        }

        public async Task<Result<UsersRole>> GetUsersRoleByNameAsync(string name)
        {
            var role = await _unitOfWork.UsersRole.GetByNameAsync(name);
            if (role == null)
            {
                return Result<UsersRole>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Nível de Acesso com nome '{name}' não encontrado."));
            }
            return Result<UsersRole>.Success(role);
        }

        public async Task<Result<IEnumerable<UsersRole>>> GetAllUsersRolesAsync()
        {
            var roles = await _unitOfWork.UsersRole.ReadAllAsync();
            return Result<IEnumerable<UsersRole>>.Success(roles);
        }

        public async Task<Result> UpdateUsersRoleAsync(UsersRole updateRole)
        {
            var existingRole = await _unitOfWork.UsersRole.ReadByIdAsync(updateRole.UsersRoleId);
            if (existingRole == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Nível de Acesso com ID {updateRole.UsersRoleId} não encontrado."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (!existingRole.RoleName.Equals(updateRole.RoleName, StringComparison.Ordinal))
                {
                    if (await _unitOfWork.UsersRole.GetByNameAsync(updateRole.RoleName) != null)
                    {
                        _unitOfWork.Rollback();
                        return Result.Failure(
                            Error.Validation(
                                $"O nome do Nível de Acesso '{updateRole.RoleName}' já está em uso."));
                    }

                    existingRole.UpdateName(updateRole.RoleName);
                }

                await _unitOfWork.UsersRole.UpdateAsync(existingRole);
                await _unitOfWork.CommitAsync();

                return Result.Success("Nível de Acesso atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar nível de acesso: {ex.Message}"));
            }
        }

        public async Task<Result> DeleteUsersRoleAsync(int id)
        {
            var existingRole = await _unitOfWork.UsersRole.ReadByIdAsync(id);
            if (existingRole == null)
            {
                return Result.Success($"Nível de Acesso com ID {id} não encontrado (idempotente).");
            }

            // Opcional: verificar se há utilizadores com este role
            // var inUse = await _unitOfWork.Users.AnyWithRoleIdAsync(id);
            // if (inUse) return Result.Failure(Error.BusinessRuleViolation(...));

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.UsersRole.RemoveAsync(existingRole);
                await _unitOfWork.CommitAsync();

                return Result.Success("Nível de Acesso eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar nível de acesso: {ex.Message}"));
            }
        }
    }
}
