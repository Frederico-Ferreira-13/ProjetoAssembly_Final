using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IUserSettingsService
    {
        Task<Result<UserSettings>> GetSettingsByUserIdAsync(int userId);
        Task<Result> UpdateUserSettingsAsync(UserSettings settings);
        Task<Result<UserSettings>> CreateDefaultSettingsAsync(int userId);
    }
}
