using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var existing = await _categoryRepository.GetCategoryByNameAndAccount(categoryName, accountId);
            return existing != null && (excludeCategoryId == null || existing.CategoriesId != excludeCategoryId.Value);
        }

        public async Task<Result<Category>> CreateCategoryAsync(Category newCategory)
        {
            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful)
            {
                return Result<Category>.Failure(accountIdResult.Error);
            }

            int accountId = accountIdResult.Value;

            if (string.IsNullOrWhiteSpace(newCategory.CategoryName))
            {
                return Result<Category>.Failure(
                    Error.Validation("O nome da categoria é obrigatório.", new Dictionary<string, string[]> { { nameof(newCategory.CategoryName), new[] { "Campo obrigatório" } } })
                );
            }

            if (newCategory.CategoryName.Length > 255)
            {
                return Result<Category>.Failure(
                    Error.Validation("O nome da categoria não pode exceder 255 caracteres.", new Dictionary<string, string[]> { { nameof(newCategory.CategoryName), new[] { "Máximo 255 caracteres" } } })
                );
            }

            if (await CategoryNameExistsForAccountAsync(newCategory.CategoryName, accountId))
            {
                return Result<Category>.Failure(
                    Error.Conflict(
                    ErrorCodes.AlreadyExists,
                    $"Já existe uma categoria ativa com o nome '{newCategory.CategoryName}' para a sua conta.",
                    new Dictionary<string, string[]> { { nameof(newCategory.CategoryName), new[] 
                    { "O nome da categoria já está em uso." } } })
                );
            }

            if (newCategory.CategoryTypeId <= 0)
            {
                return Result<Category>.Failure(
                    Error.Validation("Tipo de categoria inválido.", 
                    new Dictionary<string, string[]> { { nameof(newCategory.CategoryTypeId), new[] { "ID inválido" } } })
                );
            }

            var categoryType = await _unitOfWork.CategoryType.ReadByIdAsync(newCategory.CategoryTypeId);
            if (categoryType == null)
            {
                return Result<Category>.Failure(
                    Error.NotFound(ErrorCodes.NotFound, $"Tipo de categoria com ID {newCategory.CategoryTypeId} não encontrado.")
                );
            }

            if (newCategory.ParentCategoryId.HasValue)
            {
                if (newCategory.ParentCategoryId.Value == 0)
                {
                    return Result<Category>.Failure(
                        Error.Validation("Categoria pai inválida.", new Dictionary<string, string[]> { { nameof(newCategory.ParentCategoryId), new[] { "ID inválido" } } })
                    );
                }

                var parent = await _categoryRepository.ReadByIdAndAccountAsync(newCategory.ParentCategoryId.Value, accountId);
                if (parent == null)
                {
                    return Result<Category>.Failure(
                        Error.Validation($"Categoria pai com ID {newCategory.ParentCategoryId.Value} não existe ou não pertence à sua conta.", new Dictionary<string, string[]> { { nameof(newCategory.ParentCategoryId), new[] { "Categoria pai inválida" } } })
                    );
                }
            }

            await _unitOfWork.BeginTransactionAsync();

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
                _unitOfWork.Rollback();

                string fieldName = ex.ParamName ?? "Geral";
                return Result<Category>.Failure(
                    Error.Validation("Dados inválidos para criar categoria", new Dictionary<string, string[]>
                    {
                        { ex.ParamName ?? "Geral", new[] { ex.Message } }
                    })
                );
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Category>.Failure(
                    Error.InternalServer($"Erro inesperado ao criar categoria: {ex.Message}"));
            }
        }

        public async Task<Result<Category>> GetCategoryByIdAsync(int categoryId)
        {
            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful)
            {
                return Result<Category>.Failure(accountIdResult.Error);
            }

            var category = await _categoryRepository.ReadByIdAndAccountAsync(categoryId, accountIdResult.Value);
            if (category == null)
            {
                return Result<Category>.Failure(
                    Error.NotFound(ErrorCodes.NotFound, $"Categoria com ID {categoryId} não encontrada ou não pertence à sua conta.")
                );
            }

            return Result<Category>.Success(category);
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

            if (!string.Equals(existingCategory.CategoryName, updateCategory.CategoryName, StringComparison.OrdinalIgnoreCase))
            {
                if (await CategoryNameExistsForAccountAsync(updateCategory.CategoryName, accountId, updateCategory.CategoriesId))
                {
                    return Result<Category>.Failure(
                        Error.Conflict(
                            ErrorCodes.AlreadyExists,
                            $"Já existe outra categoria com o nome '{updateCategory.CategoryName}'.",
                            new Dictionary<string, string[]> { { nameof(updateCategory.CategoryName), new[] { "Nome já em uso" } } }
                        )
                    );
                }
            }

            if (updateCategory.ParentCategoryId.HasValue)
            {
                if (updateCategory.ParentCategoryId.Value == existingCategory.CategoriesId)
                {
                    return Result<Category>.Failure(
                        Error.BusinessRuleViolation(
                            ErrorCodes.BizInvalidOperation,
                            "Uma categoria não pode ser a sua própria pai.",
                            new Dictionary<string, string[]> { { nameof(updateCategory.ParentCategoryId), new[] { "Ciclo inválido" } } }
                        )
                    );
                }

                var parent = await _categoryRepository.ReadByIdAndAccountAsync(updateCategory.ParentCategoryId.Value, accountId);
                if (parent == null)
                {
                    return Result<Category>.Failure(
                        Error.Validation(
                            $"Categoria pai com ID {updateCategory.ParentCategoryId.Value} não existe ou não pertence à sua conta.",
                            new Dictionary<string, string[]> { { nameof(updateCategory.ParentCategoryId), new[] { "Categoria pai inválida" } } }
                        )
                    );
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingCategory.UpdateDetails(
                    newCategoryName: updateCategory.CategoryName,
                    newCategoryTypeId: updateCategory.CategoryTypeId,
                    newParentCategoryId: updateCategory.ParentCategoryId
                );

                await _unitOfWork.Category.UpdateAsync(existingCategory);
                await _unitOfWork.CommitAsync();

                return Result<Category>.Success(existingCategory);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();

                return Result<Category>.Failure(Error.Validation("Dados inválidos para atualizar categoria", new Dictionary<string, string[]>
                {
                    { ex.ParamName ?? "Geral", new[] { ex.Message } }
                }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Category>.Failure(
                    Error.InternalServer($"Erro inesperado ao atualizar categoria: {ex.Message}"));
            }
        }

        public async Task<Result<IEnumerable<Category>>> GetCategoriesByUserIdAsync()
        {
            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful)
            {
                return Result<IEnumerable<Category>>.Failure(accountIdResult.Error);
            }

            var categories = await _categoryRepository.GetRootCategoriesByAccount(accountIdResult.Value);
            return Result<IEnumerable<Category>>.Success(categories);
        }

        public async Task<Result<IEnumerable<Category>>> GetUserActiveCategoriesAsync()
        {
            return await GetCategoriesByUserIdAsync();
        }

        public async Task<bool> CategoryNameExistsForUserAsync(string categoryName, int userId, int? excludeCategoryId = null)
        {
            var accountIdResult = await GetCurrentAccountIdAsync();
            if (!accountIdResult.IsSuccessful) return false;

            return await CategoryNameExistsForAccountAsync(categoryName, accountIdResult.Value, excludeCategoryId);
        }    

        public async Task<Result<CategoryType>> GetCategoryTypeByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<CategoryType>.Failure(
                    Error.Validation(
                        "Nome do tipo de categoria é obrigatório.",
                        new Dictionary<string, string[]> { { nameof(name), new[] { "Campo obrigatório" } } }
                    )
                );
            }

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

        public async Task<Result<IEnumerable<CategoryType>>> GetAllCategoryTypesAsync()
        {
            var categoryTypes = await _unitOfWork.CategoryType.ReadAllAsync();
            return Result<IEnumerable<CategoryType>>.Success(categoryTypes);
        }

        public async Task<Result<CategoryType>> CreateCategoryTypeAsync(CategoryType categoryType)
        {
            if (string.IsNullOrWhiteSpace(categoryType.TypeName))
            {
                return Result<CategoryType>.Failure(
                     Error.Validation(
                         "O nome do tipo de categoria é obrigatório.",
                         new Dictionary<string, string[]> { { nameof(categoryType.TypeName), new[] { "Campo obrigatório" } } }
                     )
                 );
            }

            if (categoryType.TypeName.Length > 50)
            {
                return Result<CategoryType>.Failure(
                    Error.Validation(
                        "O nome do tipo de categoria não pode exceder 50 caracteres.",
                        new Dictionary<string, string[]> { { nameof(categoryType.TypeName), new[] { "Máximo 50 caracteres" } } }
                    )
                );
            }

            if (await _unitOfWork.CategoryType.GetByNameAsync(categoryType.TypeName) != null)
            {
                return Result<CategoryType>.Failure(
                    Error.Validation(
                    $"O Tipo de Categoria '{categoryType.TypeName}' já existe.",
                    new Dictionary<string, string[]> { { nameof(categoryType.TypeName), new[] { "Nome já em uso." } } })
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newCategoryType = new CategoryType(categoryType.TypeName);
                await _unitOfWork.CategoryType.CreateAddAsync(newCategoryType);
                await _unitOfWork.CommitAsync();

                return Result<CategoryType>.Success(newCategoryType);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();

                string fieldName = ex.ParamName ?? "Geral";
                return Result<CategoryType>.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para o tipo de categoria.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();

                return Result<CategoryType>.Failure(
                    Error.InternalServer($"Erro inesperado ao criar tipo de categoria: {ex.Message}"));
            }
        }

        public async Task<Result<CategoryType>> UpdateCategoryTypeAsync(CategoryType updateCategoryType)
        {
            var existingType = await _unitOfWork.CategoryType.ReadByIdAsync(updateCategoryType.CategoryTypeId);
            if (existingType == null)
            {
                return Result<CategoryType>.Failure(
                    Error.NotFound(ErrorCodes.NotFound, $"Tipo de categoria com ID {updateCategoryType.CategoryTypeId} não encontrado.")
                );
            }

            if (!string.Equals(existingType.TypeName, updateCategoryType.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (await _unitOfWork.CategoryType.GetByNameAsync(updateCategoryType.TypeName) != null)
                {
                    return Result<CategoryType>.Failure(
                        Error.Conflict(
                            ErrorCodes.AlreadyExists,
                            $"O nome '{updateCategoryType.TypeName}' já está em uso.",
                            new Dictionary<string, string[]> { { nameof(updateCategoryType.TypeName), new[] { "Nome já em uso" } } }
                        )
                    );
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingType.UpdateName(updateCategoryType.TypeName);
                await _unitOfWork.CategoryType.UpdateAsync(existingType);
                await _unitOfWork.CommitAsync();

                return Result<CategoryType>.Success(existingType);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                return Result<CategoryType>.Failure(
                    Error.Validation("Dados inválidos para atualizar tipo de categoria.", 
                    new Dictionary<string, string[]> { { ex.ParamName ?? "Geral", new[] { ex.Message } } })
                );
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<CategoryType>.Failure(
                    Error.InternalServer($"Erro inesperado ao atualizar tipo de categoria: {ex.Message}"));
            }
        }

        public async Task<Result> DeleteCategoryTypeAsync(int id)
        {
            var existingType = await _unitOfWork.CategoryType.ReadByIdAsync(id);
            if (existingType == null)
            {
                return Result.Success();
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.CategoryType.RemoveAsync(existingType);
                await _unitOfWork.CommitAsync();
                return Result.Success("Tipo de Categoria eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar tipo de categoria: {ex.Message}"));
            }
        }
    }
}
