using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class RecipeService : IRecipesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecipesRepository _recipesRepository;
        private readonly ITokenService _tokenService;

        public RecipeService(IUnitOfWork unitOfWork, IRecipesRepository recipesRepository,
            ITokenService tokenService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _recipesRepository = recipesRepository ?? throw new ArgumentNullException(nameof(recipesRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<bool> IsRecipeOwnerAsync(int recipeId)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();

            if (!userIdResult.IsSuccessful)
            {
                return false;
            }

            int currentUserId = userIdResult.Value;
            var recipe = await _recipesRepository.ReadByIdAsync(recipeId);

            return recipe != null && recipe.UserId == currentUserId;
        }

        public async Task<bool> ExistsAsync(int recipeId)
        {
            if (recipeId <= 0)
            {
                return false;
            }

            return await _recipesRepository.ExistsByIdAsync(recipeId);
        }

        public async Task<Result<Recipes>> GetRecipeByIdAsync(int recipeId)
        {
            var recipe = await _recipesRepository.ReadByIdAsync(recipeId);

            if (recipe == null)
            {
                return Result<Recipes>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Receita com ID {recipeId} não encontrada.")
                );
            }

            return Result<Recipes>.Success(recipe);
        }

        public async Task<Result<IEnumerable<Recipes>>> GetAllRecipesAsync()
        {
            var recipes = await _recipesRepository.ReadAllAsync();
            return Result<IEnumerable<Recipes>>.Success(recipes);
        }

        public async Task<Result<Recipes>> CreateRecipeAsync(Recipes newRecipe)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();

            if (!userIdResult.IsSuccessful)
            {
                return Result<Recipes>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "O utilizador deve estar autenticado para criar uma receita.")
                );
            }

            int currentUserId = userIdResult.Value;

            try
            {
                var recipesToCreate = new Recipes(
                    userId: currentUserId,
                    categoriesId: newRecipe.CategoriesId,
                    difficultyId: newRecipe.DifficultyId,
                    title: newRecipe.Title,
                    instructions: newRecipe.Instructions,
                    prepTimeMinutes: newRecipe.PrepTimeMinutes,
                    cookTimeMinutes: newRecipe.CookTimeMinutes,
                    servings: newRecipe.Servings
                );

                await _recipesRepository.CreateAddAsync(recipesToCreate);
                await _unitOfWork.CommitAsync();

                return Result<Recipes>.Success(recipesToCreate);
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Recipes>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a receita.",
                        new Dictionary<string, string[]>
                        {
                            { fieldName, new string[] { ex.Message } }
                        }
                    )
                );
            }

        }

        public async Task<Result> UpdateRecipeAsync(Recipes recipeToUpdate)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();

            if (!userIdResult.IsSuccessful)
            {
                return Result.Failure(Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "O utilizador deve estar autenticado para atualizar uma receita.")
                );
            }

            int currentUserId = userIdResult.Value;

            var existingRecipe = await _recipesRepository.ReadByIdAsync(recipeToUpdate.RecipesId);

            if (existingRecipe == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Receita com ID {recipeToUpdate.RecipesId} não encontrada.")
                );
            }

            if (existingRecipe.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "O Utilizador não tem permissão para atualizar esta receita. Apenas o criador pode fazê-lo.")
                );
            }

            try
            {
                existingRecipe.UpdateDetails(
                    newTitle: recipeToUpdate.Title,
                    newInstructions: recipeToUpdate.Instructions,
                    newPrepTime: recipeToUpdate.PrepTimeMinutes,
                    newCookTime: recipeToUpdate.CookTimeMinutes,
                    newServings: recipeToUpdate.Servings
                );

                existingRecipe.ChangeCategory(recipeToUpdate.CategoriesId);

                await _recipesRepository.UpdateAsync(existingRecipe);
                await _unitOfWork.CommitAsync();

                return Result.Success("Receita atualizada com sucesso.");
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a atualização da receita.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }
        }

        public async Task<Result> DeleteRecipeAsync(int recipeId)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();

            if (!userIdResult.IsSuccessful)
            {
                return Result.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "O utilizador deve estar autenticado para eliminar uma receita.")
                );
            }

            int currentUserId = userIdResult.Value;


            var existingRecipe = await _recipesRepository.ReadByIdAsync(recipeId);

            if (existingRecipe == null)
            {
                return Result.Success("Receita não encontrada ou já eliminada.");
            }

            if (existingRecipe.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "O Utilizador não tem permissão para eliminar esta receita. Apenas o criador pode fazê-lo.")
                );
            }

            await _recipesRepository.RemoveAsync(existingRecipe);
            await _unitOfWork.CommitAsync();

            return Result.Success("Receita eliminada com sucesso.");
        }
    }
}
