using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IDifficultyRepository : IRepository<Difficulty>
    {
        Task<Difficulty?> GetByNameAsync(string difficultyName);
    }
}
