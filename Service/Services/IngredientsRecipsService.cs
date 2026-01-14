using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class IngredientsRecipsService : IIngredientsRecipesService
    {
        private readonly IIngredientsRecipsRepository _ingredientsRecipsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecipesService _recipesService;
        private readonly IUsersService _usersService;

        public IngredientsRecipsService(IIngredientsRecipsRepository ingredientsRecipsRepository,
            IUnitOfWork unitOfWork, IRecipesService recipesService, IUsersService usersService)
        {
            _ingredientsRecipsRepository = ingredientsRecipsRepository ?? throw new ArgumentNullException(nameof(ingredientsRecipsRepository));            
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _recipesService = recipesService ?? throw new ArgumentNullException(nameof(recipesService));
            _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        }

        public async Task<Result<IngredientsRecips>> GetIngredientsRecipsIdAsync(int id)
        {
            var ingredientRecip = await _ingredientsRecipsRepository.ReadByIdAsync(id);

            if (ingredientRecip == null)
            {
                return Result<IngredientsRecips>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"A ligação Ingrediente/Receita com ID {id} não foi encontrada.")
                );
            }

            return Result<IngredientsRecips>.Success(ingredientRecip);
        }

        public async Task<Result<IEnumerable<IngredientsRecips>>> GetIngredientsRecipsByRecipeIdAsync(int recipeId)
        {
            if (!await _unitOfWork.Recipes.ExistsByIdAsync(recipeId))
            {
                return Result<IEnumerable<IngredientsRecips>>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"A Receita com ID {recipeId} não existe.")
                );
            }

            var ingredientsRecips = await _ingredientsRecipsRepository.GetByRecipesIdAsync(recipeId);
            return Result<IEnumerable<IngredientsRecips>>.Success(ingredientsRecips);
        }

        public async Task<Result<IEnumerable<IngredientsRecips>>> GetAllIngredientsRecipsAsync()
        {
            var ingredientsRecips = await _ingredientsRecipsRepository.ReadAllAsync();
            return Result<IEnumerable<IngredientsRecips>>.Success(ingredientsRecips);
        }

        public async Task<Result<IngredientsRecips>> CreateIngredientsRecipesAsync(IngredientsRecips newIngredientsRecipes)
        {
            if (!await _unitOfWork.Recipes.ExistsByIdAsync(newIngredientsRecipes.RecipesId))
            {
                return Result<IngredientsRecips>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"A Receita com ID {newIngredientsRecipes.RecipesId} não foi encontrada.",
                    new Dictionary<string, string[]> { { nameof(newIngredientsRecipes.RecipesId), new[] { "Receita inválida ou inexistente." } } })
                );
            }

            var isOwnerResult = await _recipesService.IsRecipeOwnerAsync(newIngredientsRecipes.RecipesId);

            if (!isOwnerResult)
            {
                return Result<IngredientsRecips>.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "Não tem permissão para adicionar ingredientes a esta receita. Apenas o criador pode fazê-lo.")
                );
            }

            if (await _unitOfWork.Ingredients.ReadByIdAsync(newIngredientsRecipes.IngredientsId) == null)
            {
                return Result<IngredientsRecips>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"O Ingrediente com ID {newIngredientsRecipes.IngredientsId} não foi encontrado.",
                    new Dictionary<string, string[]> { { nameof(newIngredientsRecipes.IngredientsId), new[] { "Ingrediente inválido ou inexistente." } } })
                );
            }

            if (await _ingredientsRecipsRepository.IsIngredientUsedInRecipeAsync(newIngredientsRecipes.RecipesId, newIngredientsRecipes.IngredientsId))
            {
                return Result<IngredientsRecips>.Failure(Error.Validation(
                    "O Ingrediente já foi adicionado a esta receita. Use o Update para alterar a quantidade.",
                    new Dictionary<string, string[]> { { nameof(newIngredientsRecipes.IngredientsId), new[] { "Ingrediente duplicado." } } })
                );
            }

            try
            {
                var newIngredientRecipes = new IngredientsRecips(
                    recipesId: newIngredientsRecipes.RecipesId,
                    ingredientsId: newIngredientsRecipes.IngredientsId,
                    quantityValue: newIngredientsRecipes.QuantityValue,
                    unit: newIngredientsRecipes.Unit
                );

                await _ingredientsRecipsRepository.CreateAddAsync(newIngredientRecipes);
                await _unitOfWork.CommitAsync();

                return Result<IngredientsRecips>.Success(newIngredientRecipes);
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result<IngredientsRecips>.Failure(Error.Validation(
                    "Dados de entrada inválidos para o ingrediente da receita.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }
        }

        public async Task<Result> UpdateIngredientsRecipesAsync(int id, IngredientsRecips ingredientsRecipesToUpdate)
        {
            var existinRecip = await _ingredientsRecipsRepository.ReadByIdAsync(id);
            if (existinRecip == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"O ingrediente da receita com o ID {id} não foi encontrado.")
                );
            }

            var isOwner = await _recipesService.IsRecipeOwnerAsync(existinRecip.RecipesId);
            if (!isOwner)
            {
                return Result.Failure(Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "Não tem permissão para atualizar ingredientes desta receita. Apenas o criador pode fazê-lo.")
                );
            }

            try
            {
                existinRecip.Update(ingredientsRecipesToUpdate.QuantityValue, ingredientsRecipesToUpdate.Unit);

                await _ingredientsRecipsRepository.UpdateAsync(existinRecip);
                await _unitOfWork.CommitAsync();

                return Result.Success("Ingrediente da receita atualizado com sucesso.");
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(Error.Validation(
                    "Dados de entrada inválidos para o ingrediente da receita.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }
        }

        public async Task<Result> DeleteIngredientsRecipsAsync(int id)
        {
            var existingRecip = await _ingredientsRecipsRepository.ReadByIdAsync(id);

            if (existingRecip == null)
            {
                return Result.Failure(Error.NotFound(
                    ErrorCodes.NotFound,
                    $"O ingrediente da receita com o ID {id} não foi encontrado para eliminação.")
                );
            }

            var isOwner = await _recipesService.IsRecipeOwnerAsync(existingRecip.RecipesId);
            if (!isOwner)
            {
                return Result.Failure(Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "Não tem permissão para eliminar ingredientes desta receita. Apenas o criador pode fazê-lo.")
                );
            }

            await _ingredientsRecipsRepository.RemoveAsync(existingRecip);
            await _unitOfWork.CommitAsync();

            return Result.Success("Ingrediente da receita eliminado com sucesso.");
        }
    }
}
