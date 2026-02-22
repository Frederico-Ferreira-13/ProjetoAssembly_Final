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
    public class RecipeService : IRecipesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecipesRepository _recipesRepository;
        private readonly ITokenService _tokenService;
        private readonly IFavoritesRepository _favoritesRepository;

        public RecipeService(IUnitOfWork unitOfWork, IRecipesRepository recipesRepository,
            ITokenService tokenService, IFavoritesRepository favoritesRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _recipesRepository = recipesRepository ?? throw new ArgumentNullException(nameof(recipesRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _favoritesRepository = favoritesRepository ?? throw new ArgumentNullException(nameof(favoritesRepository));
        }

        public async Task<bool> IsRecipeFavoriteAsync(int recipeId, int userId)
        {
            return await _favoritesRepository.ExistsAsync(recipeId, userId);
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

        public async Task<Result<IEnumerable<Recipes>>> GetRecipesByUserIdAsync(int userId)
        {
            try
            {
                var allrecipes = await _recipesRepository.ReadAllAsync();
                var userRecipes = allrecipes.Where(r => r.UserId == userId).ToList();
                
                return Result<IEnumerable<Recipes>>.Success(userRecipes);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Recipes>>.Failure(
                    Error.InternalServer($"Erro ao obter receitas do utilizador: {ex.Message}"));
            }
        }

        public async Task<Result<IEnumerable<Recipes>>> GetAllRecipesAsync()
        {
            var recipes = await _recipesRepository.ReadAllAsync();
            return Result<IEnumerable<Recipes>>.Success(recipes);
        }

        public async Task<int> GetFavoriteCountAsync(int recipeId)
        {
            return await _favoritesRepository.GetCountByRecipeIdAsync(recipeId);
        }

        public async Task<int> GetTotalRecipesByUserAsync(int userId)
        {
            var recipes = await _recipesRepository.ReadAllAsync();
            return recipes.Count(r => r.UserId == userId);
        }

        public async Task<int> GetTotalFavoritesByUserAsync(int userId)
        {
            var favorites = await _favoritesRepository.GetByUserIdAsync(userId);
            return favorites.Count();
        }

        public async Task<Result<IEnumerable<Recipes>>> GetPendingRecipesAsync()
        {
            var pending = await _recipesRepository.GetPendingRecipesAsync();
            return Result<IEnumerable<Recipes>>.Success(pending);
        }

        public async Task<Result<(IEnumerable<Recipes> Items, int TotalCount)>> SearchRecipesAsync(string? search, int? categoryId, int page, int pageSize, int? currentUserId)
        {
            try
            {
                var data = await _recipesRepository.SearchRecipesAsync(search, categoryId, page, pageSize, currentUserId);
                return Result<(IEnumerable<Recipes> Items, int TotalCount)>.Success(data);
            }
            catch (Exception ex)
            {
                return Result<(IEnumerable<Recipes> Items, int TotalCount)>.Failure(
                    Error.InternalServer($"Erro ao pesquisar receitas: {ex.Message}"));
            }
        }

        public async Task<Result<object>> ToggleFavoriteAsync(int recipeId, int userId)
        {
            var exists = await _recipesRepository.ExistsByIdAsync(recipeId);
            if (!exists)
            {
                return Result<object>.Failure(
                    Error.NotFound("Rec_01", "Receita não encontrada"));
            }

            var currentUserIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userId)
            {
                return Result<object>.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Não pode togglear favorito para outro utilizador."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var isFavorite = await _favoritesRepository.ExistsAsync(recipeId, userId);
                if (isFavorite)
                {
                    await _favoritesRepository.DeleteFavoriteAsync(recipeId, userId);
                }
                else
                {
                    var newFav = new Favorites(userId, recipeId);
                    await _favoritesRepository.CreateAddAsync(newFav);
                }

                await _unitOfWork.CommitAsync();

                var newCount = await _favoritesRepository.GetCountByRecipeIdAsync(recipeId);
                return Result<object>.Success(new
                {
                    isFavorite = !isFavorite,
                    newCount = newCount
                });
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<object>.Failure(
                    Error.InternalServer($"Erro ao togglear favorito: {ex.Message}"));
            }

        }

        public async Task<Result<Recipes>> CreateRecipeAsync(Recipes newRecipe)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Result<Recipes>.Failure(
                    Error.Unauthorized(
                        ErrorCodes.AuthUnauthorized,
                        "O utilizador deve estar autenticado para criar uma receita."));
            }

            int currentUserId = userIdResult.Value;

            var user = await _unitOfWork.Users.ReadByIdAsync(currentUserId);
            bool isAdmin = user != null && user.UsersRoleId == 1;

            await _unitOfWork.BeginTransactionAsync();

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

                if (isAdmin)
                {
                    recipesToCreate.Approve();
                }                

                await _recipesRepository.CreateAddAsync(recipesToCreate);
                await _unitOfWork.CommitAsync();

                return Result<Recipes>.Success(recipesToCreate);
            }            
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Recipes>.Failure(
                    Error.InternalServer($"Erro ao criar receita: {ex.Message}"));
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
                        $"Receita com ID {recipeToUpdate.RecipesId} não encontrada."));
            }

            if (existingRecipe.UserId != currentUserId)
            {
                return Result.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "O Utilizador não tem permissão para atualizar esta receita. Apenas o criador pode fazê-lo.")
                );
            }

            await _unitOfWork.BeginTransactionAsync();

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
                existingRecipe.ChangeDifficulty(recipeToUpdate.DifficultyId);

                if (!string.IsNullOrEmpty(recipeToUpdate.ImageUrl))
                {
                    existingRecipe.SetImageUrl(recipeToUpdate.ImageUrl);
                }

                await _recipesRepository.UpdateAsync(existingRecipe);
                await _unitOfWork.CommitAsync();

                return Result.Success("Receita atualizada com sucesso.");
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(
                    Error.Validation(
                        "Dados de entrada inválidos para a atualização da receita.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao atualizar receita: {ex.Message}"));
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
                        "O utilizador deve estar autenticado para eliminar uma receita."));
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
                        "O Utilizador não tem permissão para eliminar esta receita. Apenas o criador pode fazê-lo."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _recipesRepository.RemoveAsync(existingRecipe);
                await _unitOfWork.CommitAsync();

                return Result.Success("Receita eliminada com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao eliminar receita: {ex.Message}"));
            }
        }

        public async Task<Result> ApproveRecipeAsync(int recipeId)
        {
            var recipe = await _recipesRepository.ReadByIdAsync(recipeId);
            if(recipe == null)
            {
                return Result.Failure(Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Receita com ID {recipeId} não encontrada."));
            }

            /*var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Result.Failure(Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "Não autenticado."));
            }

            var user = await _unitOfWork.Users.ReadByIdAsync(userIdResult.Value);
            if(user == null || user.UsersRoleId != 1)
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas administradores podem aprovar receitas."));
            }*/

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                recipe.Approve();
                await _recipesRepository.UpdateAsync(recipe);
                await _unitOfWork.CommitAsync();

                return Result.Success("Receita aprovada com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(
                    Error.InternalServer($"Erro ao aprovar receita: {ex.Message}"));
            }
        }
    }
}
