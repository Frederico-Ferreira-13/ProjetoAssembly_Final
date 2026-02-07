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

            try
            {
                var newDifficulty = new Difficulty(difficulty.DifficultyName);
                await _unitOfWork.Difficulty.CreateAddAsync(newDifficulty);
                await _unitOfWork.CommitAsync(); // Commit

                return Result<Difficulty>.Success(newDifficulty);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Difficulty>.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para a dificuldade.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Difficulty>.Failure(
                    Error.InternalServer($"Erro inesperado ao criar dificuldade: {ex.Message}"));
            }
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

            await _unitOfWork.BeginTransactionAsync(); // Início da transação

            try
            {
                bool changed = false;

                if (existingDifficulty.DifficultyName != updateDifficulty.DifficultyName)
                {
                    if (await _unitOfWork.Difficulty.GetByNameAsync(updateDifficulty.DifficultyName) != null)
                    {
                        _unitOfWork.Rollback(); // Rollback precoce se conflito
                        return Result.Failure(
                            Error.Validation(
                                $"O nome da Dificuldade '{updateDifficulty.DifficultyName}' já está em uso.",
                                new Dictionary<string, string[]> { { nameof(updateDifficulty.DifficultyName), new[] { "Nome já em uso." } } }));
                    }

                    existingDifficulty.UpdateName(updateDifficulty.DifficultyName);
                    changed = true;
                }

                if (changed)
                {
                    await _unitOfWork.Difficulty.UpdateAsync(existingDifficulty);
                    await _unitOfWork.CommitAsync(); // Commit só se houve mudança
                }

                return Result.Success("Dificuldade atualizada com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para a atualização da dificuldade.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar dificuldade: {ex.Message}"));
            }
        }

        public async Task<Result> DeleteDifficultyAsync(int id)
        {
            var existingDifficulty = await _unitOfWork.Difficulty.ReadByIdAsync(id);

            if (existingDifficulty == null)
            {
                return Result.Success($"Dificuldade com ID {id} não encontrada (Idempotência).");
            }

            var inUse = await _unitOfWork.Recipes.AnyWithDifficultyIdAsync(id);
            if (inUse)
            {
                return Result.Failure(
                    Error.BusinessRuleViolation(
                        ErrorCodes.BizHasDependencies,
                        "Não é possível eliminar uma dificuldade que está em uso por receitas."));
            }

            await _unitOfWork.BeginTransactionAsync(); // Início da transação

            try
            {
                await _unitOfWork.Difficulty.RemoveAsync(existingDifficulty);
                await _unitOfWork.CommitAsync(); // Commit

                return Result.Success("Dificuldade eliminada com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar dificuldade: {ex.Message}"));
            }
        }
    }
}
