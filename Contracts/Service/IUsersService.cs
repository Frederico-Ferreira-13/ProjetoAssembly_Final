using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IUsersService
    {
        Task<Result<Users>> RegisterUserAsync(Users newUser, string password);
        Task<Result<Users>> AuthenticateUserAsync(string identifier, string password);
        Task<Result<Users>> GetUserByIdAsync(int userId);
        Task<Result<Users>> GetUserByEmailAsync(string email);
        Task<Result> UpdateUserProfileAsync(Users userToUpdate);
        Task<Result> ChangeUserPasswordAsync(int userId, string oldPassword, string newPassword);
        Task<Result> DeactivateUserAsync(int userId);
        Task<Result> ActivateUserAsync(int userId);
        Task<Result> DeleteUserAsync(int userId);
        Task<Result<Users>> GetUserByUsernameOrEmailAsync(string identifier);
        Task<bool> UserExistsByEmailAsync(string email);
        Task<bool> IsUserNameUniqueAsync(string userName);
        Task<bool> UserExistsAsync(int userId);
        Task<Result<int>> GetCurrentUserIdAsync();
        Task<Result<Users>> GetCurrentUserAsync();

        Task<Result<UserSettings>> GetSettingsByUserIdAsync(int userId);
        Task<Result> UpdateUserSettingsAsync(UserSettings settings);
        Task<Result<UserSettings>> CreateDefaultSettingsAsync(int userId);

        Task<Result<UsersRole>> CreateUsersRoleAsync(UsersRole dto);
        Task<Result<UsersRole>> GetUsersRoleByIdAsync(int id);
        Task<Result<UsersRole>> GetUsersRoleByNameAsync(string name);
        Task<Result<IEnumerable<UsersRole>>> GetAllUsersRolesAsync();
        Task<Result> UpdateUsersRoleAsync(UsersRole updateUserRole);
        Task<Result> DeleteUsersRoleAsync(int id);
    }
}
