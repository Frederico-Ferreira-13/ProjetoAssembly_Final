using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using Microsoft.Extensions.Configuration;
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
        private readonly HashSet<string> _adminEmails;

        public UsersService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, 
            IAuthenticationService authenticationService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));                       
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

            var adminEmailsSection = configuration.GetSection("AdminEmails");
            _adminEmails = new HashSet<string>(
                adminEmailsSection.GetChildren()
                    .Select(child => child.Value?.Trim() ?? "")
                    .Where(v => !string.IsNullOrWhiteSpace(v)),
                StringComparer.OrdinalIgnoreCase  // ignora maiúsculas/minúsculas
            );
        }

        public async Task<Result<Users>> RegisterUserAsync(string userName, string name, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || userName.Length < 3 || userName.Length > 50)
            {
                return Result<Users>.Failure(
                    Error.Validation(
                        "Nome de utilizador inválido (3-50 caracteres).",
                        new Dictionary<string, string[]> { { nameof(userName), new[] { "Mínimo 3, máximo 50 caracteres" } } }
                    )
                );

            }

            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
            {
                return Result<Users>.Failure(
                    Error.Validation(
                        "Nome inválido (2-100 caracteres).",
                        new Dictionary<string, string[]> { { nameof(name), new[] { "Mínimo 2, máximo 100 caracteres" } } }
                    )
                );
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || email.Length > 100)
            {
                return Result<Users>.Failure(
                    Error.Validation(
                        "Email inválido.",
                        new Dictionary<string, string[]> { { nameof(email), new[] { "Formato de email inválido" } } }
                    )
                );
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                return Result<Users>.Failure(
                    Error.Validation(
                        "Password inválida (mínimo 8 caracteres).",
                        new Dictionary<string, string[]> { { nameof(password), new[] { "Mínimo 8 caracteres" } } }
                    )
                );
            }

            if (await _unitOfWork.Users.GetUserByUserNameAsync(userName) != null)
            {
                return Result<Users>.Failure(
                    Error.Conflict(
                        ErrorCodes.AlreadyExists,
                        "Este nome de utilizador já está em uso.",
                        new Dictionary<string, string[]> { { nameof(userName), new[] { "Escolha outro nome de utilizador" } } }
                    )
                );
            }

            if (await _unitOfWork.Users.GetByEmailAsync(email) != null)
            {
                return Result<Users>.Failure(
                    Error.Validation("Este eail já está registado. Faça login ou use outro."));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await _unitOfWork.Users.GetByEmailAsync(normalizedEmail) != null)
            {
                return Result<Users>.Failure(
                    Error.Conflict(
                        ErrorCodes.AlreadyExists,
                        "Este email já está registado.",
                        new Dictionary<string, string[]> { { nameof(email), new[] { "Use outro email ou faça login" } } }
                    )
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var salt = _passwordHasher.GenerateSalt();
                var hashResult = _passwordHasher.HashPassword(password, salt);
                if (!hashResult.IsSuccessful)
                {                    
                    return Result<Users>.Failure(hashResult.Error);
                }

                bool isAdminEmail = _adminEmails.Contains(email.ToLowerInvariant());

                var userToSave = new Users(
                    name: name,
                    userName: userName,                    
                    email: email,                    
                    usersRoleId: isAdminEmail ? 1 : 2,
                    isApproved: isAdminEmail,
                    accountId: 1
                );

                userToSave.SetPassword(hashResult.Value.Hash, salt);

                await _unitOfWork.Users.CreateAddAsync(userToSave);
                await CreateDefaultSettingsAsync(userToSave.UserId);

                await _unitOfWork.CommitAsync();

                return Result<Users>.Success(userToSave);
            }           
            catch (Exception ex)
            {                
                if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate key"))
                {
                    return Result<Users>.Failure(
                        Error.Conflict(
                            ErrorCodes.AlreadyExists,
                            "Este email ou nome de utilizador já existe."
                        )
                    );
                }

                return Result<Users>.Failure(
                    Error.InternalServer($"Erro ao registar utilizador: {ex.Message}")
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

            var user = await _unitOfWork.Users.ReadByIdAsync(authResult.Value!.UserId);
            if (user == null || !user.IsActive)
            {
                return Result<Users>.Failure(
                    Error.Unauthorized(
                        ErrorCodes.AuthUnauthorized,
                        "Conta inativa ou não encontrada."
                    )
                );
            }

            return Result<Users>.Success(user);
        }

        public async Task<Result<Users>> GetUserByIdAsync(int userId)
        {
            if (userId <= 0)
            {
                return Result<Users>.Failure(
                    Error.Validation(
                        "ID do utilizador inválido.",
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null)
            {
                return Result<Users>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Utilizador com ID {userId} não encontrado."
                    )
                );
            }

            return Result<Users>.Success(user);
        }

        public async Task<Result<Users>> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result<Users>.Failure(
                    Error.Validation(
                        "Email inválido.",
                        new Dictionary<string, string[]> { { nameof(email), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
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
                        $"Utilizador com ID {userToUpdate.UserId} não encontrado."
                    )
                );
            }

            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userToUpdate.UserId)
            {
                return Result.Failure(
                     Error.Forbidden(
                         ErrorCodes.AuthForbidden,
                         "Apenas o próprio utilizador pode atualizar o seu perfil."
                     )
                 );
            }

            if (!string.IsNullOrWhiteSpace(userToUpdate.UserName) && userToUpdate.UserName != existingUser.UserName)
            {
                if (await _unitOfWork.Users.GetUserByUserNameAsync(userToUpdate.UserName) != null)
                {
                    return Result.Failure(
                        Error.Conflict(
                            ErrorCodes.AlreadyExists,
                            "Este nome de utilizador já está em uso.",
                            new Dictionary<string, string[]> { { nameof(userToUpdate.UserName), new[] { "Escolha outro" } } }
                        )
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(userToUpdate.Email) && userToUpdate.Email != existingUser.Email)
            {
                var normalizedEmail = userToUpdate.Email.Trim().ToLowerInvariant();
                if (await _unitOfWork.Users.GetByEmailAsync(normalizedEmail) != null)
                {
                    return Result.Failure(
                        Error.Conflict(
                            ErrorCodes.AlreadyExists,
                            "Este email já está registado.",
                            new Dictionary<string, string[]> { { nameof(userToUpdate.Email), new[] { "Use outro email" } } }
                        )
                    );
                }
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

                if (userToUpdate.ProfilePicture != existingUser.ProfilePicture)
                {
                    existingUser.UpdateProfilePicture(userToUpdate.ProfilePicture);
                }

                await _unitOfWork.Users.UpdateAsync(existingUser);
                await _unitOfWork.CommitAsync();

                return Result.Success("Perfil atualizado com sucesso.");
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
                        new Dictionary<string, string[]> { { "Password", new[] { "Passwords não podem ser nulas." } } }
                    )
                );
            }

            if (newPassword.Length < 8)
            {
                return Result.Failure(
                    Error.Validation(
                        "A nova password deve ter pelo menos 8 caracteres.",
                        new Dictionary<string, string[]> { { nameof(newPassword), new[] { "Mínimo 8 caracteres" } } }
                    )
                );
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        "Utilizador não encontrado ou inativo."
                    )
                );
            }

            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userId)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas o próprio utilizador pode alterar a sua password."
                    )
                );
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
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao alterar password: {ex.Message}"));
            }
        }

        public async Task<Result> DeactivateUserAsync(int userId)
        {
            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userId)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas o próprio utilizador pode desativar a sua conta."
                    )
                );
            }

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
            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful)
            {
                return Result.Failure(Error.Unauthorized(ErrorCodes.AuthUnauthorized, "Não autenticado."));
            }

            var currentUser = await _unitOfWork.Users.ReadByIdAsync(currentUserIdResult.Value);
            if (currentUser == null || currentUser.UsersRoleId != 1) // 1 = Admin
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas administradores podem ativar contas."
                    )
                );
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(userId);
            if (user == null)
            {
                return Result.Success("Utilizador não encontrado (idempotente).");
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
            // Restringir a admins ou próprio utilizador
            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful)
            {
                return Result.Failure(Error.Unauthorized(ErrorCodes.AuthUnauthorized, "Não autenticado."));
            }

            var currentUser = await _unitOfWork.Users.ReadByIdAsync(currentUserIdResult.Value);
            if (currentUser == null || (currentUserIdResult.Value != userId && currentUser.UsersRoleId != 1))
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas o próprio utilizador ou administradores podem eliminar contas."
                    )
                );
            }

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
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Result<Users>.Failure(
                    Error.Validation(
                        "Identificador inválido.",
                        new Dictionary<string, string[]> { { nameof(identifier), new[] { "Campo obrigatório" } } }
                    )
                );
            }          

            var normalized = identifier.Trim().ToLowerInvariant();
            var user = await _unitOfWork.Users.GetByEmailAsync(normalized);
            if (user == null)
            {
                user = await _unitOfWork.Users.GetUserByUserNameAsync(normalized);
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
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            var normalized = email.Trim().ToLowerInvariant();
            return await _unitOfWork.Users.GetByEmailAsync(normalized) != null;
        }

        public async Task<bool> IsUserNameUniqueAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) 
            {
                return false;
            } 
            return await _unitOfWork.Users.GetUserByUserNameAsync(userName) == null;
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            if(userId <= 0)
            {
                return false;
            }
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
            if (userId <= 0)
            {
                return Result<UserSettings>.Failure(
                    Error.Validation(
                        "ID do utilizador inválido.",
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

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

            var currentUserIdResult = await GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != settings.UserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas o próprio utilizador pode atualizar as suas configurações."
                    )
                );
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
            if (userId <= 0)
            {
                return Result<UserSettings>.Failure(
                    Error.Validation(
                        "ID do utilizador inválido.",
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var userExists = await _unitOfWork.Users.ReadByIdAsync(userId) != null;
            if (!userExists)
            {
                return Result<UserSettings>.Failure(
                    Error.BusinessRuleViolation(
                        ErrorCodes.BizInvalidOperation,
                        $"Utilizador com ID {userId} não existe. Não é possível criar configurações."));
            }

            var existing = await _unitOfWork.UserSettings.GetByUserId(userId);
            if (existing != null)
            {
                return Result<UserSettings>.Failure(
                    Error.Conflict(
                        ErrorCodes.AlreadyExists,
                        $"Configurações para o utilizador {userId} já existem."
                    )
                );
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
            if (string.IsNullOrWhiteSpace(dto.RoleName) || dto.RoleName.Length > 50)
            {
                return Result<UsersRole>.Failure(
                    Error.Validation(
                        "Nome do nível de acesso inválido (obrigatório, máximo 50 caracteres).",
                        new Dictionary<string, string[]> { { nameof(dto.RoleName), new[] { "Campo inválido" } } }
                    )
                );
            }

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
            if (id <= 0)
            {
                return Result<UsersRole>.Failure(
                    Error.Validation("ID do nível de acesso inválido.")
                );
            }

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
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<UsersRole>.Failure(
                    Error.Validation("Nome do nível de acesso obrigatório.")
                );
            }

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

            if (string.IsNullOrWhiteSpace(updateRole.RoleName) || updateRole.RoleName.Length > 50)
            {
                return Result.Failure(
                    Error.Validation(
                        "Nome do nível de acesso inválido (obrigatório, máximo 50 caracteres)."
                    )
                );
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
                            Error.Conflict(
                                ErrorCodes.AlreadyExists,
                                $"O nome '{updateRole.RoleName}' já está em uso."
                            )
                        );
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
