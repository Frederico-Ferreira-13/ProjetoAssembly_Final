using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthenticationService _authService;
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(IUnitOfWork unitOfWork, IAuthenticationService authService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _categoryRepository = _unitOfWork.Category;
        }

        private async Task<Result<int>> GetCurrentAccountIdAsync()
        {
            var userResult = await _authService.GetPersistedUserAsync();

            if (!userResult.IsSuccessful)
            {
                return Result<int>.Failure(userResult.Error);
            }

            var user = userResult.Value;

            if (user == null)
            {
                return Result<int>.Failure(
                    Error.Unauthorized(
                   ErrorCodes.AuthUnauthorized,
                   "Dados de utilizador autenticado não encontrados.")
               );
            }

            return Result<int>.Success(user.AccountId);
        }

        private async Task<bool> CategoryNameExistsForAccountAsync(string categoryName, int accountId, int? excludeCategoryId = null)
        {
            var existingCategory = await _categoryRepository.GetCategoryByNameAndAccount(categoryName, accountId);
            return existingCategory != null && existingCategory.IsActive && (!excludeCategoryId.HasValue || existingCategory.CategoriesId != excludeCategoryId.Value);
        }

        public async Task<Result<Category>> CreateCategoryAsync(Category newCategory)
        {
            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful)
            {
                return Result<Category>.Failure(accountIdResult.Error);
            }

            int accountId = accountIdResult.Value;

            if (await CategoryNameExistsForAccountAsync(newCategory.CategoryName, accountId))
            {
                return Result<Category>.Failure(
                    Error.Conflict(
                    ErrorCodes.AlreadyExists,
                    $"Já existe uma categoria ativa com o nome '{newCategory.CategoryName}' para a sua conta.",
                    new Dictionary<string, string[]> { { nameof(newCategory.CategoryName), new[] { "O nome da categoria já está em uso." } } })
                );
            }

            if (newCategory.ParentCategoryId.HasValue)
            {
                var parentCategory = await _unitOfWork.Category.ReadByIdAndAccountAsync(newCategory.ParentCategoryId.Value, accountId);
                if (parentCategory == null || !parentCategory.IsActive)
                {
                    return Result<Category>.Failure(
                        Error.Validation(
                        $"A categoria pai com ID {newCategory.ParentCategoryId.Value} não existe ou está inativa.",
                        new Dictionary<string, string[]> { { nameof(newCategory.ParentCategoryId), new[] { "Categoria pai inválida." } } })
                    );
                }
            }

            try
            {
                var categoryToSave = new Category(
                categoryName: newCategory.CategoryName,
                categoryTypeId: newCategory.CategoryTypeId,
                accountId: accountId,
                parentCategoryId: newCategory.ParentCategoryId
                );

                await _unitOfWork.Category.CreateAddAsync(categoryToSave);
                await _unitOfWork.CommitAsync();

                return Result<Category>.Success(categoryToSave);
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Category>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a categoria.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }

        }

        public async Task<Result<Category>> GetCategoryByIdAsync(int categoryId)
        {
            var accountId = await GetCurrentAccountIdAsync();
            if (!accountId.IsSuccessful)
            {
                return Result<Category>.Failure(accountId.Error);
            }

            var category = await _categoryRepository.ReadByIdAndAccountAsync(categoryId, accountId.Value);

            if (category == null)
            {
                return Result<Category>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Categoria com ID {categoryId} não encontrada.")
                );
            }

            return Result<Category>.Success(category);
        }

        public async Task<Result<IEnumerable<Category>>> GetCategoriesByUserIdAsync()
        {
            var accountId = await GetCurrentAccountIdAsync();
            if (!accountId.IsSuccessful)
            {
                return Result<IEnumerable<Category>>.Failure(accountId.Error);
            }

            var categories = await _categoryRepository.GetRootCategoriesByAccount(accountId.Value);
            return Result<IEnumerable<Category>>.Success(categories);
        }

        public async Task<Result<IEnumerable<Category>>> GetUserActiveCategoriesAsync()
        {
            var accountId = await GetCurrentAccountIdAsync();
            if (!accountId.IsSuccessful)
            {
                return Result<IEnumerable<Category>>.Failure(accountId.Error);
            }

            var categories = await _categoryRepository.GetRootCategoriesByAccount(accountId.Value);
            var activeCategories = categories.Where(c => c.IsActive);

            return Result<IEnumerable<Category>>.Success(activeCategories);
        }

        public async Task<bool> CategoryNameExistsForUserAsync(string categoryName, int userId, int? excludeCategoryId = null)
        {
            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful) return false;

            return await CategoryNameExistsForAccountAsync(categoryName, accountIdResult.Value, excludeCategoryId);
        }

        public async Task<Result<Category>> UpdateCategoryAsync(Category updateCategory)
        {

            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful)
            {
                return Result<Category>.Failure(accountIdResult.Error);
            }
            int accountId = accountIdResult.Value;

            var existingCategory = await _categoryRepository.ReadByIdAndAccountAsync(updateCategory.CategoriesId, accountId);
            if (existingCategory == null)
            {
                return Result<Category>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Categoria com ID {updateCategory.CategoriesId} não encontrada ou não pertence à sua conta.")
                );
            }

            if (!existingCategory.CategoryName.Equals(updateCategory.CategoryName, StringComparison.OrdinalIgnoreCase))
            {
                if (await CategoryNameExistsForAccountAsync(updateCategory.CategoryName, accountId, updateCategory.CategoriesId))
                {
                    return Result<Category>.Failure(
                        Error.Conflict(
                        ErrorCodes.AlreadyExists,
                        $"Já existe outra categoria ativa com o nome '{updateCategory.CategoryName}' para esta conta.",
                        new Dictionary<string, string[]> { { nameof(updateCategory.CategoryName), new[] { "O nome da categoria já está em uso." } } }
                    ));
                }
            }

            if (updateCategory.ParentCategoryId.HasValue)
            {
                if (updateCategory.ParentCategoryId.Value == existingCategory.CategoriesId)
                {
                    return Result<Category>.Failure(
                        Error.BusinessRuleViolation(
                        ErrorCodes.BizInvalidOperation,
                        "Uma categoria não pode ser a sua própria categoria pai.",
                        new Dictionary<string, string[]> { { nameof(updateCategory.ParentCategoryId), new[] { "Referência recursiva não permitida." } } })
                    );
                }

                var parentCategory = await _categoryRepository.ReadByIdAndAccountAsync(updateCategory.ParentCategoryId.Value, accountId);
                if (parentCategory == null || !parentCategory.IsActive)
                {
                    return Result<Category>.Failure(
                        Error.Validation(
                        $"A nova categoria pai com ID {updateCategory.ParentCategoryId.Value} não existe, está inativa ou não pertence à sua conta.",
                        new Dictionary<string, string[]> { { nameof(updateCategory.ParentCategoryId), new[] { "Categoria pai inválida." } } })
                    );
                }
            }

            try
            {
                existingCategory.UpdateDetails(
                newCategoryName: updateCategory.CategoryName,
                newCategoryTypeId: updateCategory.CategoryTypeId, // Assumindo que adicionou Type ao UpdateCategoryDTO
                newParentCategoryId: updateCategory.ParentCategoryId
                );

                await _unitOfWork.Category.UpdateAsync(existingCategory);
                await _unitOfWork.CommitAsync();

                return Result<Category>.Success(existingCategory);
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Category>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a atualização da categoria.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }

        }

        public async Task<Result<Category>> DeactivateCategoryAsync(int categoryId)
        {
            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful)
            {
                return Result<Category>.Failure(accountIdResult.Error);
            }
            int accountId = accountIdResult.Value;

            var categoryToDeactivate = await _categoryRepository.GetByIdWithSubCategories(categoryId, accountId);

            if (categoryToDeactivate == null)
            {
                return Result<Category>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Categoria com ID {categoryId} não encontrada ou não pertence à sua conta.")
                );
            }

            if (categoryToDeactivate.SubCategories != null && categoryToDeactivate.SubCategories.Any(c => c.IsActive))
            {
                return Result<Category>.Failure(
                    Error.BusinessRuleViolation(
                    ErrorCodes.BizHasDependencies,
                    "Não é possível desativar uma categoria que tem subcategorias ativas.")
                );
            }

            if (!categoryToDeactivate.IsActive)
            {                
                return Result<Category>.Success(categoryToDeactivate, "A categoria já se encontra desativada.");
            }

            // 3. Desativar no Modelo de Domínio e Persistir
            categoryToDeactivate.Deactivate();
            await _unitOfWork.Category.UpdateAsync(categoryToDeactivate);
            await _unitOfWork.CommitAsync();

            return Result<Category>.Success(categoryToDeactivate);
        }
    }
}
