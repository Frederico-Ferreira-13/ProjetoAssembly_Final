using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<Difficulty>.Failure(
                    Error.Validation(
                        "O nome da dificuldade é obrigatório.",
                        new Dictionary<string, string[]> { { nameof(name), new[] { "Campo obrigatório" } } }
                    )
                );
            }

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

        public async Task<Result<Difficulty>> CreateDifficultyAsync(Difficulty newDifficulty)
        {
            if (string.IsNullOrWhiteSpace(newDifficulty.DifficultyName))
            {
                return Result<Difficulty>.Failure(
                    Error.Validation(
                        "O nome da dificuldade é obrigatório.",
                        new Dictionary<string, string[]> { { nameof(newDifficulty.DifficultyName), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            if (newDifficulty.DifficultyName.Length > 50)
            {
                return Result<Difficulty>.Failure(
                    Error.Validation(
                        "O nome da dificuldade não pode exceder 50 caracteres.",
                        new Dictionary<string, string[]> { { nameof(newDifficulty.DifficultyName), new[] { "Máximo 50 caracteres" } } }
                    )
                );
            }

            if (await _unitOfWork.Difficulty.GetByNameAsync(newDifficulty.DifficultyName) != null)
            {
                return Result<Difficulty>.Failure(
                    Error.Conflict(
                        ErrorCodes.AlreadyExists,
                        $"A dificuldade '{newDifficulty.DifficultyName}' já existe.",
                        new Dictionary<string, string[]> { { nameof(newDifficulty.DifficultyName), new[] { "Nome já em uso" } } }
                    )
                );
            }

            try
            {
                var difficultyToCreate = new Difficulty(newDifficulty.DifficultyName);

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Difficulty.CreateAddAsync(difficultyToCreate);
                await _unitOfWork.CommitAsync(); // Commit

                return Result<Difficulty>.Success(difficultyToCreate);
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

            if (string.IsNullOrWhiteSpace(updateDifficulty.DifficultyName))
            {
                return Result.Failure(
                    Error.Validation(
                        "O nome da dificuldade é obrigatório.",
                        new Dictionary<string, string[]> { { nameof(updateDifficulty.DifficultyName), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            if (updateDifficulty.DifficultyName.Length > 50)
            {
                return Result.Failure(
                    Error.Validation(
                        "O nome da dificuldade não pode exceder 50 caracteres.",
                        new Dictionary<string, string[]> { { nameof(updateDifficulty.DifficultyName), new[] { "Máximo 50 caracteres" } } }
                    )
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
                        _unitOfWork.Rollback();
                        return Result.Failure(
                            Error.Conflict(
                                ErrorCodes.AlreadyExists,
                                $"O nome '{updateDifficulty.DifficultyName}' já está em uso.",
                                new Dictionary<string, string[]> { { nameof(updateDifficulty.DifficultyName), new[] { "Nome já em uso" } } }
                            )
                        );
                    }

                    existingDifficulty.UpdateName(updateDifficulty.DifficultyName);
                    changed = true;
                }

                if (changed)
                {
                    await _unitOfWork.Difficulty.UpdateAsync(existingDifficulty);
                    await _unitOfWork.CommitAsync();
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
                        "Não é possível eliminar uma dificuldade que está em uso por receitas.")
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.Difficulty.RemoveAsync(existingDifficulty);
                await _unitOfWork.CommitAsync();

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