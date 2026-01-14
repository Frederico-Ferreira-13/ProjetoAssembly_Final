using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class CategoryTypeService : ICategoryTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<CategoryType>> CreateCategoryTypeAsync(CategoryType categoryType)
        {
            if (await _unitOfWork.CategoryType.GetByNameAsync(categoryType.TypeName) != null)
            {
                return Result<CategoryType>.Failure(
                    Error.Validation(
                    $"O Tipo de Categoria '{categoryType.TypeName}' já existe.",
                    new Dictionary<string, string[]> { { nameof(categoryType.TypeName), new[] { "Nome já em uso." } } })
                );
            }

            var newCategoryType = new CategoryType(categoryType.TypeName);

            await _unitOfWork.CategoryType.CreateAddAsync(newCategoryType);
            await _unitOfWork.CommitAsync();

            return Result<CategoryType>.Success(newCategoryType);
        }

        public async Task<Result<CategoryType>> GetCategoryTypeByIdAsync(int id)
        {
            var categoryType = await _unitOfWork.CategoryType.ReadByIdAsync(id);

            if (categoryType == null)
            {
                return Result<CategoryType>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Tipo de Categoria com ID {id} não encontrado.")
                );
            }

            return Result<CategoryType>.Success(categoryType);
        }

        public async Task<Result<CategoryType>> GetCategoryTypeByNameAsync(string name)
        {
            var categoryType = await _unitOfWork.CategoryType.GetByNameAsync(name);

            if (categoryType == null)
            {
                return Result<CategoryType>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Tipo de Categoria com nome '{name}' não encontrado.")
                );
            }

            return Result<CategoryType>.Success(categoryType);
        }

        public async Task<Result<IEnumerable<CategoryType>>> GetAllCategoryTypesAsync()
        {
            var categoryTypes = await _unitOfWork.CategoryType.ReadAllAsync();           

            return Result<IEnumerable<CategoryType>>.Success(categoryTypes);
        }

        public async Task<Result> UpdateCategoryTypeAsync(CategoryType updateCategoryType)
        {
            var existingType = await _unitOfWork.CategoryType.ReadByIdAsync(updateCategoryType.CategoryTypeId);

            if (existingType == null)
            {
                return Result.Failure(Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Tipo de Categoria com ID {updateCategoryType.CategoryTypeId} não encontrado.")
                );
            }

            string newTypeName = updateCategoryType.TypeName;

            if (existingType.TypeName != updateCategoryType.TypeName)
            {
                if (await _unitOfWork.CategoryType.GetByNameAsync(updateCategoryType.TypeName) != null)
                {
                    return Result.Failure(
                        Error.Validation(
                        $"O nome do Tipo de Categoria '{updateCategoryType.TypeName}' já está em uso.")
                    );
                }

                existingType.UpdateName(newTypeName);

                await _unitOfWork.CategoryType.UpdateAsync(existingType);
            }

            await _unitOfWork.CommitAsync();

            return Result.Success("Tipo de Categoria atualizado com sucesso.");
        }

        public async Task<Result> DeleteCategoryTypeAsync(int id)
        {
            var existingType = await _unitOfWork.CategoryType.ReadByIdAsync(id);

            if (existingType == null)
            {
                return Result.Success($"Tipo de Categoria com ID {id} não encontrado (Idempotência).");
            }

            await _unitOfWork.CategoryType.RemoveAsync(existingType);
            await _unitOfWork.CommitAsync();

            return Result.Success("Tipo de Categoria eliminado com sucesso.");
        }
    }
}
