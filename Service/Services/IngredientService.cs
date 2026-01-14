using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class IngredientService : IIngredientsService
    {
        private readonly IIngredientsRepository _ingredientsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIngredientsRecipsRepository _ingredientsRecipsRepository;

        public IngredientService(IIngredientsRepository ingredientsRepository, IUnitOfWork unitOfWork,
                                 IIngredientsRecipsRepository ingredientsRecipsRepository)
        {
            _ingredientsRepository = ingredientsRepository ?? throw new ArgumentNullException(nameof(ingredientsRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));            
            _ingredientsRecipsRepository = ingredientsRecipsRepository ?? throw new ArgumentNullException(nameof(ingredientsRecipsRepository));
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
                return Result<Ingredients>.Failure
                    (Error.Validation(
                    $"O Ingrediente '{newIngredient.IngredientName}' já existe.",
                    new Dictionary<string, string[]> { { nameof(newIngredient.IngredientName), new[] { "Este nome já está em uso." } } })
                );
            }

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
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Ingredients>.Failure(Error.Validation(
                    "Dados de entrada inválidos para a criação do ingrediente.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
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
                    $"O Ingrediente com ID {ingredientToUpdate.IngredientsId} não foi encontrado para atualização.")
                );
            }

            if (!await _ingredientsRepository.IsIngredientUnique(ingredientToUpdate.IngredientName, ingredientToUpdate.IngredientsId))
            {
                return Result.Failure(Error.Validation(
                    $"O Ingrediente '{ingredientToUpdate.IngredientName}' já existe.",
                    new Dictionary<string, string[]> { { nameof(ingredientToUpdate.IngredientName), new[] { "Este nome já está em uso." } } })
                );
            }

            try
            {
                existingIngredient.UpdateDetails(ingredientToUpdate.IngredientName, ingredientToUpdate.IngredientsTypeId);

                await _ingredientsRepository.UpdateAsync(existingIngredient);
                await _unitOfWork.CommitAsync();

                return Result.Success("Ingrediente atualizado com sucesso.");
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(Error.Validation(
                    "Dados de entrada inválidos para a atualização do ingrediente.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
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
                    "Não é possível excluir o ingrediente porque ele está associado a uma ou mais receitas.")
                );
            }

            await _ingredientsRepository.RemoveAsync(existingIngredient);
            await _unitOfWork.CommitAsync();

            return Result.Success("Ingrediente eliminado com sucesso.");
        }
    }
}
