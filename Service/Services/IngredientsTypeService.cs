using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class IngredientsTypeService : IIngredientsTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public IngredientsTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<IngredientsType>> CreateIngredientsTypeAsync(IngredientsType ingredientsType)
        {
            if (await _unitOfWork.IngredientsType.GetByNameAsync(ingredientsType.IngredientsTypeName) != null)
            {
                return Result<IngredientsType>.Failure(Error.Validation(
                    $"O Tipo de Ingrediente '{ingredientsType.IngredientsTypeName}' já existe.",
                    new Dictionary<string, string[]> { { nameof(ingredientsType.IngredientsTypeName), new[] { "Nome já em uso." } } })
                );
            }

            var newIngredientType = new IngredientsType(ingredientsType.IngredientsTypeName);

            await _unitOfWork.IngredientsType.CreateAddAsync(newIngredientType);
            await _unitOfWork.CommitAsync();

            return Result<IngredientsType>.Success(newIngredientType);
        }

        public async Task<Result<IngredientsType>> GetIngredientsTypeByIdAsync(int id)
        {
            var ingredientsType = await _unitOfWork.IngredientsType.ReadByIdAsync(id);

            if (ingredientsType == null)
            {
                return Result<IngredientsType>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Tipo de Ingrediente com ID {id} não encontrado.")
                );
            }

            return Result<IngredientsType>.Success(ingredientsType);
        }

        public async Task<Result<IngredientsType>> GetIngredientsTypeByNameAsync(string name)
        {
            var ingredientsType = await _unitOfWork.IngredientsType.GetByNameAsync(name);

            if (ingredientsType == null)
            {
                return Result<IngredientsType>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound, $"Tipo de Ingrediente com nome '{name}' não encontrado.")
                );
            }

            return Result<IngredientsType>.Success(ingredientsType);
        }

        public async Task<Result<IEnumerable<IngredientsType>>> GetAllIngredientsTypesAsync()
        {
            var ingredientsTypes = await _unitOfWork.IngredientsType.ReadAllAsync();            

            return Result<IEnumerable<IngredientsType>>.Success(ingredientsTypes);
        }

        public async Task<Result> UpdateIngredientsTypeAsync(IngredientsType updateIngredientsType)
        {
            var existingType = await _unitOfWork.IngredientsType.ReadByIdAsync(updateIngredientsType.IngredientsTypeId);

            if (existingType == null)
            {
                return Result.Failure(Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Tipo de Ingrediente com ID {updateIngredientsType.IngredientsTypeId} não encontrado.")
                );
            }

            string newTypeName = updateIngredientsType.IngredientsTypeName;

            if (existingType.IngredientsTypeName != newTypeName)
            {
                if (await _unitOfWork.IngredientsType.GetByNameAsync(newTypeName) != null)
                {
                    return Result.Failure(
                        Error.Validation(
                        $"O nome do Tipo de Ingrediente '{newTypeName}' já está em uso.")
                    );
                }

                existingType.UpdateName(newTypeName);

                await _unitOfWork.IngredientsType.UpdateAsync(existingType);
            }

            await _unitOfWork.CommitAsync();

            return Result.Success("Tipo de Ingrediente atualizado com sucesso.");
        }

        public async Task<Result> DeleteIngredientsTypeAsync(int id)
        {
            var existingType = await _unitOfWork.IngredientsType.ReadByIdAsync(id);

            if (existingType == null)
            {
                return Result.Success($"Tipo de Ingrediente com ID {id} não encontrado (Idempotência).");
            }

            await _unitOfWork.IngredientsType.RemoveAsync(existingType);
            await _unitOfWork.CommitAsync();

            return Result.Success("Tipo de Ingrediente eliminado com sucesso.");
        }
    }
}
