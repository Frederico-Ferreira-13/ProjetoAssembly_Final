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
    public class IngredientService : IIngredientsService
    {
        private readonly IIngredientsRepository _ingredientsRepository;
        private readonly IIngredientsTypeRepository _ingredientsTypeRepository;
        private readonly IIngredientsRecipsRepository _ingredientsRecipsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecipesService _recipesService;
        private readonly IUsersService _usersService;        

        public IngredientService(IIngredientsRepository ingredientsRepository, IIngredientsTypeRepository ingredientsTypeRepository,
            IUnitOfWork unitOfWork, IIngredientsRecipsRepository ingredientsRecipsRepository, IRecipesService recipesService, 
            IUsersService usersService)
        {
            _ingredientsRepository = ingredientsRepository ?? throw new ArgumentNullException(nameof(ingredientsRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));            
            _ingredientsRecipsRepository = ingredientsRecipsRepository ?? throw new ArgumentNullException(nameof(ingredientsRecipsRepository));
            _ingredientsTypeRepository = ingredientsTypeRepository ??  throw new ArgumentNullException(nameof(ingredientsTypeRepository));
            _recipesService = recipesService ?? throw new ArgumentNullException(nameof(_recipesService));
            _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        }

        public async Task<Result<Ingredients>> GetIngredientByIdAsync(int ingredientId)
        {
            var ingredient = await _ingredientsRepository.ReadByIdAsync(ingredientId);

            if (ingredient == null)
            {
                return Result<Ingredients>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"O Ingrediente com ID {ingredientId} não foi encontrado.")
                );
            }

            return Result<Ingredients>.Success(ingredient);
        }

        public async Task<Result<IEnumerable<Ingredients>>> SearchIngredientsAsync(string searchIngredient)
        {
            var ingredients = await _ingredientsRepository.Search(searchIngredient);
            return Result<IEnumerable<Ingredients>>.Success(ingredients);
        }

        public async Task<Result<Ingredients>> CreateIngredientAsync(Ingredients newIngredient)
        {
            if (await _ingredientsRepository.IsIngredientUnique(newIngredient.IngredientName))
            {
                return Result<Ingredients>.Failure(
                    Error.Validation(
                        $"O Ingrediente '{newIngredient.IngredientName}' já existe.",
                        new Dictionary<string, string[]> { { nameof(newIngredient.IngredientName), new[] { "Este nome já está em uso." } } }));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var ingredientToCreate = new Ingredients(
                    ingredientName: newIngredient.IngredientName,
                    ingredientsTypeId: newIngredient.IngredientsTypeId
                );

                await _ingredientsRepository.CreateAddAsync(ingredientToCreate);
                await _unitOfWork.CommitAsync();

                return Result<Ingredients>.Success(ingredientToCreate);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Ingredients>.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para a criação do ingrediente.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Ingredients>.Failure(
                    Error.InternalServer($"Erro ao criar ingrediente: {ex.Message}"));
            }
        }

        public async Task<Result> UpdateIngredientAsync(Ingredients ingredientToUpdate)
        {
            var existingIngredient = await _ingredientsRepository.ReadByIdAsync(ingredientToUpdate.IngredientsId);
            if (existingIngredient == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"O Ingrediente com ID {ingredientToUpdate.IngredientsId} não foi encontrado para atualização."));
            }

            if (!await _ingredientsRepository.IsIngredientUnique(ingredientToUpdate.IngredientName, ingredientToUpdate.IngredientsId))
            {
                return Result.Failure(Error.Validation(
                        $"O Ingrediente '{ingredientToUpdate.IngredientName}' já existe.",
                        new Dictionary<string, string[]> { { nameof(ingredientToUpdate.IngredientName), new[] { "Este nome já está em uso." } } }));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingIngredient.UpdateDetails(ingredientToUpdate.IngredientName, ingredientToUpdate.IngredientsTypeId);
                await _ingredientsRepository.UpdateAsync(existingIngredient);
                await _unitOfWork.CommitAsync();

                return Result.Success("Ingrediente atualizado com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(Error.Validation(
                    "Dados de entrada inválidos para a atualização do ingrediente.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
        }

        public async Task<Result> DeleteIngredientAsync(int ingredientId)
        {
            var existingIngredient = await _ingredientsRepository.ReadByIdAsync(ingredientId);
            if (existingIngredient == null)
            {
                return Result.Success("Ingrediente não encontrado ou já eliminado.");
            }

            if (await _ingredientsRecipsRepository.IsIngredientUsedInAnyRecipeAsync(ingredientId))
            {
                return Result.Failure(
                    Error.BusinessRuleViolation(
                        ErrorCodes.BizHasDependencies,
                        "Não é possível excluir o ingrediente porque ele está associado a uma ou mais receitas."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _ingredientsRepository.RemoveAsync(existingIngredient);
                await _unitOfWork.CommitAsync();

                return Result.Success("Ingrediente eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar ingrediente: {ex.Message}"));
            }
        }

        public async Task<Result<IngredientsType>> GetIngredientsTypeByIdAsync(int id)
        {
            var type = await _ingredientsTypeRepository.ReadByIdAsync(id);
            if (type == null)
            {
                return Result<IngredientsType>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Tipo de Ingrediente com ID {id} não encontrado."));
            }
            return Result<IngredientsType>.Success(type);
        }

        public async Task<Result<IngredientsType>> GetIngredientsTypeByNameAsync(string name)
        {
            var type = await _ingredientsTypeRepository.GetByNameAsync(name);
            if (type == null)
            {
                return Result<IngredientsType>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Tipo de Ingrediente com nome '{name}' não encontrado."));
            }
            return Result<IngredientsType>.Success(type);
        }

        public async Task<Result<IEnumerable<IngredientsType>>> GetAllIngredientsTypesAsync()
        {
            var types = await _ingredientsTypeRepository.ReadAllAsync();
            return Result<IEnumerable<IngredientsType>>.Success(types);
        }

        public async Task<Result<IngredientsType>> CreateIngredientsTypeAsync(IngredientsType ingredientsType)
        {
            if (await _ingredientsTypeRepository.GetByNameAsync(ingredientsType.IngredientsTypeName) != null)
            {
                return Result<IngredientsType>.Failure(
                    Error.Validation(
                        $"O Tipo de Ingrediente '{ingredientsType.IngredientsTypeName}' já existe.",
                        new Dictionary<string, string[]> { { nameof(ingredientsType.IngredientsTypeName), new[] { "Nome já em uso." } } }));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newType = new IngredientsType(ingredientsType.IngredientsTypeName);
                await _ingredientsTypeRepository.CreateAddAsync(newType);
                await _unitOfWork.CommitAsync();

                return Result<IngredientsType>.Success(newType);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<IngredientsType>.Failure(
                    Error.InternalServer($"Erro ao criar tipo de ingrediente: {ex.Message}"));
            }
        }

        public async Task<Result> UpdateIngredientsTypeAsync(IngredientsType updateType)
        {
            var existingType = await _ingredientsTypeRepository.ReadByIdAsync(updateType.IngredientsTypeId);
            if (existingType == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Tipo de Ingrediente com ID {updateType.IngredientsTypeId} não encontrado."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                bool changed = false;

                if (existingType.IngredientsTypeName != updateType.IngredientsTypeName)
                {
                    if (await _ingredientsTypeRepository.GetByNameAsync(updateType.IngredientsTypeName) != null)
                    {
                        _unitOfWork.Rollback();
                        return Result.Failure(
                            Error.Validation(
                                $"O nome do Tipo de Ingrediente '{updateType.IngredientsTypeName}' já está em uso."));
                    }

                    existingType.UpdateName(updateType.IngredientsTypeName);
                    changed = true;
                }

                if (changed)
                {
                    await _ingredientsTypeRepository.UpdateAsync(existingType);
                    await _unitOfWork.CommitAsync();
                }

                return Result.Success("Tipo de Ingrediente atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar tipo de ingrediente: {ex.Message}"));
            }
        }

        public async Task<Result> DeleteIngredientsTypeAsync(int id)
        {
            var existingType = await _ingredientsTypeRepository.ReadByIdAsync(id);
            if (existingType == null)
            {
                return Result.Success($"Tipo de Ingrediente com ID {id} não encontrado (idempotente).");
            }            

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _ingredientsTypeRepository.RemoveAsync(existingType);
                await _unitOfWork.CommitAsync();

                return Result.Success("Tipo de Ingrediente eliminado com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar tipo de ingrediente: {ex.Message}"));
            }
        }

        public async Task<Result<IngredientsRecips>> GetIngredientsRecipsByIdAsync(int id)
        {
            var link = await _ingredientsRecipsRepository.ReadByIdAsync(id);
            if (link == null)
            {
                return Result<IngredientsRecips>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Ligação Ingrediente/Receita com ID {id} não encontrada."));
            }
            return Result<IngredientsRecips>.Success(link);
        }

        public async Task<Result<IEnumerable<IngredientsRecips>>> GetIngredientsByRecipeIdAsync(int recipeId)
        {
            if (!await _unitOfWork.Recipes.ExistsByIdAsync(recipeId))
            {
                return Result<IEnumerable<IngredientsRecips>>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Receita com ID {recipeId} não existe."));
            }

            var links = await _ingredientsRecipsRepository.GetByRecipesIdAsync(recipeId);
            return Result<IEnumerable<IngredientsRecips>>.Success(links);
        }

        public async Task<Result<IngredientsRecips>> AddIngredientToRecipeAsync(IngredientsRecips newLink)
        {
            if (!await _unitOfWork.Recipes.ExistsByIdAsync(newLink.RecipesId))
            {
                return Result<IngredientsRecips>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Receita com ID {newLink.RecipesId} não encontrada."));
            }

            var isOwner = await _recipesService.IsRecipeOwnerAsync(newLink.RecipesId);
            if (!isOwner)
            {
                return Result<IngredientsRecips>.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas o criador da receita pode adicionar ingredientes."));
            }

            if (await _unitOfWork.Ingredients.ReadByIdAsync(newLink.IngredientsId) == null)
            {
                return Result<IngredientsRecips>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Ingrediente com ID {newLink.IngredientsId} não encontrado."));
            }

            if (await _ingredientsRecipsRepository.IsIngredientUsedInRecipeAsync(newLink.RecipesId, newLink.IngredientsId))
            {
                return Result<IngredientsRecips>.Failure(
                    Error.Validation(
                        "Este ingrediente já foi adicionado à receita.",
                        new Dictionary<string, string[]> { { nameof(newLink.IngredientsId), new[] { "Ingrediente duplicado." } } }));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var linkToCreate = new IngredientsRecips(
                    recipesId: newLink.RecipesId,
                    ingredientsId: newLink.IngredientsId,
                    quantityValue: newLink.QuantityValue,
                    unit: newLink.Unit
                );

                await _ingredientsRecipsRepository.CreateAddAsync(linkToCreate);
                await _unitOfWork.CommitAsync();

                return Result<IngredientsRecips>.Success(linkToCreate);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result<IngredientsRecips>.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para adicionar ingrediente à receita.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<IngredientsRecips>.Failure(
                    Error.InternalServer($"Erro ao adicionar ingrediente à receita: {ex.Message}"));
            }
        }

        public async Task<Result> UpdateIngredientsInRecipeAsync(int id, IngredientsRecips updateLink)
        {
            var existingLink = await _ingredientsRecipsRepository.ReadByIdAsync(id);
            if (existingLink == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Ligação Ingrediente/Receita com ID {id} não encontrada."));
            }

            var isOwner = await _recipesService.IsRecipeOwnerAsync(existingLink.RecipesId);
            if (!isOwner)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas o criador da receita pode atualizar ingredientes."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingLink.Update(updateLink.QuantityValue, updateLink.Unit, updateLink.Detail ?? null);
                await _ingredientsRecipsRepository.UpdateAsync(existingLink);
                await _unitOfWork.CommitAsync();

                return Result.Success("Ingrediente da receita atualizado com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para atualizar ingrediente da receita.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar ingrediente da receita: {ex.Message}"));
            }
        }

        public async Task<Result> RemoveIngredientFromRecipeAsync(int id)
        {
            var existingLink = await _ingredientsRecipsRepository.ReadByIdAsync(id);
            if (existingLink == null)
            {
                return Result.Success("Ligação já removida (idempotente).");
            }

            var isOwner = await _recipesService.IsRecipeOwnerAsync(existingLink.RecipesId);
            if (!isOwner)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas o criador da receita pode remover ingredientes."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _ingredientsRecipsRepository.RemoveAsync(existingLink);
                await _unitOfWork.CommitAsync();

                return Result.Success("Ingrediente removido da receita com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao remover ingrediente da receita: {ex.Message}"));
            }
        }
    }
}
