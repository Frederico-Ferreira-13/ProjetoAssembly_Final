using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class DifficultyService : IDifficultyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DifficultyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<Difficulty>> CreateDifficultyAsync(Difficulty difficulty)
        {
            if (await _unitOfWork.Difficulty.GetByNameAsync(difficulty.DifficultyName) != null)
            {
                return Result<Difficulty>.Failure(
                    Error.Validation(
                    $"A Dificuldade '{difficulty.DifficultyName}' já existe.",
                    new Dictionary<string, string[]> { { nameof(difficulty.DifficultyName), new[] { "Nome já em uso." } } })
                );
            }

            var newDifficulty = new Difficulty(difficulty.DifficultyName);

            await _unitOfWork.Difficulty.CreateAddAsync(newDifficulty);
            await _unitOfWork.CommitAsync();

            return Result<Difficulty>.Success(newDifficulty);
        }

        public async Task<Result<Difficulty>> GetDifficultyByIdAsync(int id)
        {
            var difficulty = await _unitOfWork.Difficulty.ReadByIdAsync(id);

            if (difficulty == null)
            {
                return Result<Difficulty>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Dificuldade com ID {id} não encontrada.")
                );
            }

            return Result<Difficulty>.Success(difficulty);
        }

        public async Task<Result<Difficulty>> GetDifficultyByNameAsync(string name)
        {
            var difficulty = await _unitOfWork.Difficulty.GetByNameAsync(name);

            if (difficulty == null)
            {
                return Result<Difficulty>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Dificuldade com nome '{name}' não encontrada.")
                );
            }

            return Result<Difficulty>.Success(difficulty);
        }

        public async Task<Result<IEnumerable<Difficulty>>> GetAllDifficultiesAsync()
        {
            var difficulties = await _unitOfWork.Difficulty.ReadAllAsync();           

            return Result<IEnumerable<Difficulty>>.Success(difficulties);
        }

        public async Task<Result> UpdateDifficultyAsync(Difficulty updateDifficulty)
        {
            var existingDifficulty = await _unitOfWork.Difficulty.ReadByIdAsync(updateDifficulty.DifficultyId);

            if (existingDifficulty == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Dificuldade com ID {updateDifficulty.DifficultyId} não encontrada.")
                );
            }

            if (existingDifficulty.DifficultyName != updateDifficulty.DifficultyName)
            {
                if (await _unitOfWork.Difficulty.GetByNameAsync(updateDifficulty.DifficultyName) != null)
                {
                    return Result.Failure(
                        Error.Validation(
                        $"O nome da Dificuldade '{updateDifficulty.DifficultyName}' já está em uso.")
                    );
                }

                existingDifficulty.UpdateName(updateDifficulty.DifficultyName);

                await _unitOfWork.Difficulty.UpdateAsync(existingDifficulty);
            }

            await _unitOfWork.CommitAsync();

            return Result.Success("Dificuldade atualizada com sucesso.");
        }

        public async Task<Result> DeleteDifficultyAsync(int id)
        {
            var existingDifficulty = await _unitOfWork.Difficulty.ReadByIdAsync(id);

            if (existingDifficulty == null)
            {
                return Result.Success($"Dificuldade com ID {id} não encontrada (Idempotência).");
            }

            await _unitOfWork.Difficulty.RemoveAsync(existingDifficulty);
            await _unitOfWork.CommitAsync();

            return Result.Success("Dificuldade eliminada com sucesso.");
        }
    }
}
