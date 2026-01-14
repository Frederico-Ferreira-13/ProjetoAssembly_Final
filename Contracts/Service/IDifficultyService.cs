using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface IDifficultyService
    {
        Task<Result<Difficulty>> CreateDifficultyAsync(Difficulty difficulty);
        Task<Result<Difficulty>> GetDifficultyByIdAsync(int id);
        Task<Result<IEnumerable<Difficulty>>> GetAllDifficultiesAsync();
        Task<Result> UpdateDifficultyAsync(Difficulty updateDifficulty);
        Task<Result> DeleteDifficultyAsync(int id);
        Task<Result<Difficulty>> GetDifficultyByNameAsync(string name);
    }
}
