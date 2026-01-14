using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IUserSettingsRepository : IRepository<UserSettings>
    {
        Task<UserSettings?> GetByUserId(int userId);
        Task<IEnumerable<UserSettings>> GetByLanguageAsync(string language);
    }
}
