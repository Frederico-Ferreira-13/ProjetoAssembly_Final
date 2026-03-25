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

        public async Task<Result<Recipes>> GetRecipeByIdAsync(int recipeId, int? currentUserId)
        {
            if (recipeId <= 0)
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "ID da receita inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var recipe = await _recipesRepository.ReadByIdAsync(recipeId);
            if (recipe == null)
            {
                return Result<Recipes>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Receita com ID {recipeId} não encontrada.")
                );
            }

            recipe.FavoriteCount = await _favoritesRepository.GetCountByRecipeIdAsync(recipeId);

            if (currentUserId.HasValue)
            {
                recipe.IsFavorite = await _favoritesRepository.ExistsAsync(recipeId, currentUserId.Value);
                recipe.UserRating = await _recipesRepository.GetUserRatingAsync(recipeId, currentUserId.Value);
            }

            return Result<Recipes>.Success(recipe);
        }

        public async Task<Result<IEnumerable<Recipes>>> GetRecipesByUserIdAsync(int userId)
        {
            if (userId <= 0)
            {
                return Result<IEnumerable<Recipes>>.Failure(
                    Error.Validation(
                        "ID do utilizador inválido.",
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var userRecipes = await _recipesRepository.GetUserIdRecipes(userId);
            return Result<IEnumerable<Recipes>>.Success(userRecipes);
        }

        public async Task<Result<IEnumerable<Recipes>>> GetAllRecipesAsync()
        {
            var recipes = await _recipesRepository.ReadAllAsync();
            return Result<IEnumerable<Recipes>>.Success(recipes);
        }

        public async Task<Result<int>> GetFavoriteCountAsync(int recipeId)
        {
            if (recipeId <= 0)
            {
                return Result<int>.Failure(
                    Error.Validation(
                        "ID da receita inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var count = await _favoritesRepository.GetCountByRecipeIdAsync(recipeId);
            return Result<int>.Success(count);
        }

        public async Task<Result<int>> GetTotalRecipesByUserAsync(int userId)
        {
            if (userId <= 0)
            {
                return Result<int>.Failure(
                    Error.Validation(
                        "ID do utilizador inválido.",
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var recipes = await _recipesRepository.GetUserIdRecipes(userId);
            return Result<int>.Success(recipes.Count());
        }

        public async Task<Result<int>> GetTotalFavoritesByUserAsync(int userId)
        {
            if (userId <= 0)
            {
                return Result<int>.Failure(
                    Error.Validation(
                        "ID do utilizador inválido.",
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var count = await _favoritesRepository.GetByUserIdAsync(userId);
            return Result<int>.Success(count.Count());
        }

        public async Task<Result<IEnumerable<Recipes>>> GetPendingRecipesAsync()
        {
            var pending = await _recipesRepository.GetPendingRecipesAsync();
            return Result<IEnumerable<Recipes>>.Success(pending);
        }

        public async Task<Result<(IEnumerable<Recipes> Items, int TotalCount)>> SearchRecipesAsync(
            string? search, int? categoryId, int page, int pageSize, int? currentUserId)
        {
            if (page < 1)
            {
                return Result<(IEnumerable<Recipes> Items, int TotalCount)>.Failure(
                    Error.Validation(
                        "Número da página inválido.",
                        new Dictionary<string, string[]> { { nameof(page), new[] { "Deve ser maior ou igual a 1" } } }
                    )
                );
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return Result<(IEnumerable<Recipes> Items, int TotalCount)>.Failure(
                    Error.Validation(
                        "Tamanho da página inválido (1-100).",
                        new Dictionary<string, string[]> { { nameof(pageSize), new[] { "Valor entre 1 e 100" } } }
                    )
                );
            }

            var (items, totalCount) = await _recipesRepository.SearchRecipesAsync(search, categoryId, page, pageSize, currentUserId);
            return Result<(IEnumerable<Recipes> Items, int TotalCount)>.Success((items, totalCount));
        }

        public async Task<Result<object>> ToggleFavoriteAsync(int recipeId, int userId)
        {
            if (recipeId <= 0)
            {
                return Result<object>.Failure(
                    Error.Validation(
                        "ID da receita inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            var exists = await _recipesRepository.ExistsByIdAsync(recipeId);
            if (!exists)
            {
                return Result<object>.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Receita com ID {recipeId} não encontrada."
                    )
                );
            }

            var currentUserIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!currentUserIdResult.IsSuccessful || currentUserIdResult.Value != userId)
            {
                return Result<object>.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Não pode togglear favorito para outro utilizador."
                    )
                );
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

                return Result<object>.Success(new Dictionary<string, object>
                {
                    { "isFavorite", !isFavorite },
                    { "newCount", newCount }
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

            if (string.IsNullOrWhiteSpace(newRecipe.Title))
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "O título da receita é obrigatório.",
                        new Dictionary<string, string[]> { { nameof(newRecipe.Title), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            if (newRecipe.Title.Length > 200)
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "O título não pode exceder 200 caracteres.",
                        new Dictionary<string, string[]> { { nameof(newRecipe.Title), new[] { "Máximo 200 caracteres" } } }
                    )
                );
            }

            if (newRecipe.CategoriesId <= 0)
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "ID da categoria inválido.",
                        new Dictionary<string, string[]> { { nameof(newRecipe.CategoriesId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (newRecipe.DifficultyId <= 0)
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "ID da dificuldade inválido.",
                        new Dictionary<string, string[]> { { nameof(newRecipe.DifficultyId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (newRecipe.PrepTimeMinutes <= 0)
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "Tempo de preparação inválido.",
                        new Dictionary<string, string[]> { { nameof(newRecipe.PrepTimeMinutes), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (newRecipe.CookTimeMinutes <= 0)
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "Tempo de cozedura inválido.",
                        new Dictionary<string, string[]> { { nameof(newRecipe.CookTimeMinutes), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (string.IsNullOrWhiteSpace(newRecipe.Instructions))
            {
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "As instruções são obrigatórias.",
                        new Dictionary<string, string[]> { { nameof(newRecipe.Instructions), new[] { "Campo obrigatório" } } }
                    )
                );
            }

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

                var user = await _unitOfWork.Users.ReadByIdAsync(currentUserId);
                if (user != null && user.UsersRoleId == 1) // 1 = Admin
                {
                    recipesToCreate.Approve();
                }

                await _recipesRepository.CreateAddAsync(recipesToCreate);
                await _unitOfWork.CommitAsync();

                return Result<Recipes>.Success(recipesToCreate);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                string fieldName = ex.ParamName ?? "Geral";
                return Result<Recipes>.Failure(
                    Error.Validation(
                        "Dados inválidos para criar receita.",
                        new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }
                    )
                );
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
            var currentUser = await _unitOfWork.Users.ReadByIdAsync(currentUserId);

            var existingRecipe = await _recipesRepository.ReadByIdAsync(recipeToUpdate.RecipesId);
            if (existingRecipe == null)
            {
                return Result.Failure(
                    Error.NotFound(
                        ErrorCodes.NotFound,
                        $"Receita com ID {recipeToUpdate.RecipesId} não encontrada."));
            }

            bool isAdmin = currentUser?.UsersRoleId == 1;
            if (existingRecipe.UserId != currentUserId && !isAdmin)
            {
                return Result.Failure(
                    Error.Forbidden(
                    ErrorCodes.AuthForbidden,
                    "O Utilizador não tem permissão para atualizar esta receita. Apenas o criador pode fazê-lo.")
                );
            }

            if (string.IsNullOrWhiteSpace(recipeToUpdate.Title))
            {
                return Result.Failure(
                    Error.Validation(
                        "O título da receita é obrigatório.",
                        new Dictionary<string, string[]> { { nameof(recipeToUpdate.Title), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            if (recipeToUpdate.Title.Length > 200)
            {
                return Result.Failure(
                    Error.Validation(
                        "O título não pode exceder 200 caracteres.",
                        new Dictionary<string, string[]> { { nameof(recipeToUpdate.Title), new[] { "Máximo 200 caracteres" } } }
                    )
                );
            }

            if (recipeToUpdate.CategoriesId <= 0)
            {
                return Result.Failure(
                    Error.Validation(
                        "ID da categoria inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeToUpdate.CategoriesId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (recipeToUpdate.DifficultyId <= 0)
            {
                return Result.Failure(
                    Error.Validation(
                        "ID da dificuldade inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeToUpdate.DifficultyId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (recipeToUpdate.PrepTimeMinutes <= 0)
            {
                return Result.Failure(
                    Error.Validation(
                        "Tempo de preparação inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeToUpdate.PrepTimeMinutes), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (recipeToUpdate.CookTimeMinutes <= 0)
            {
                return Result.Failure(
                    Error.Validation(
                        "Tempo de cozedura inválido.",
                        new Dictionary<string, string[]> { { nameof(recipeToUpdate.CookTimeMinutes), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            if (string.IsNullOrWhiteSpace(recipeToUpdate.Instructions))
            {
                return Result.Failure(
                    Error.Validation(
                        "As instruções são obrigatórias.",
                        new Dictionary<string, string[]> { { nameof(recipeToUpdate.Instructions), new[] { "Campo obrigatório" } } }
                    )
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                existingRecipe.UpdateDetails(
                    recipeToUpdate.Title,
                    recipeToUpdate.Instructions,
                    recipeToUpdate.PrepTimeMinutes,
                    recipeToUpdate.CookTimeMinutes,
                    recipeToUpdate.Servings
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
            var currentUser = await _unitOfWork.Users.ReadByIdAsync(currentUserId);
            var existingRecipe = await _recipesRepository.ReadByIdAsync(recipeId);

            if (existingRecipe!.UserId != currentUserId && currentUser?.UsersRoleId != 1)
            {
                return Result.Failure(Error.Forbidden(ErrorCodes.AuthForbidden, "Sem permissão para eliminar."));
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

            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Result.Failure(
                    Error.Unauthorized(
                        ErrorCodes.AuthUnauthorized,
                        "Não autenticado."
                    )
                );
            }            

            var user = await _unitOfWork.Users.ReadByIdAsync(userIdResult.Value);
            if (user == null || user.UsersRoleId != 1) // 1 = Admin
            {
                return Result.Failure(
                    Error.Forbidden(
                        ErrorCodes.AuthForbidden,
                        "Apenas administradores podem aprovar receitas."
                    )
                );
            }

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

        public async Task<Result<IEnumerable<Recipes>>> GetRecipesWithFavoritesAsync(int? userId, int? categoryId)
        {            
            var recipes = await _recipesRepository.GetRecipesWithFavoritesAsync(userId, categoryId);

            return Result<IEnumerable<Recipes>>.Success(recipes);
        }

        public async Task<Result> UpdateRecipeRatingAsync(int recipeId, int userId, int rating)
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful || userIdResult.Value != userId)
            {
                return Result.Failure(Error.Forbidden(ErrorCodes.AuthForbidden, "Não pode avaliar em nome de outro utilizador."));
            }

            if (recipeId <= 0 || rating < 1 || rating > 5)
            {
                return Result.Failure(Error.Validation("Dados de avaliação inválidos."));
            }

            var exists = await _recipesRepository.ExistsByIdAsync(recipeId);
            if (!exists)
            {
                return Result.Failure(Error.NotFound(ErrorCodes.NotFound, $"Receita {recipeId} não encontrada."));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _recipesRepository.UpsertRecipeRatingAsync(recipeId, userId, rating);
                await _recipesRepository.UpdateRecipeAverageRatingAsync(recipeId);
                await _unitOfWork.CommitAsync();
                return Result.Success("Classificação da receita atualizada com sucesso.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Failure(Error.InternalServer($"Erro ao atualizar rating: {ex.Message}"));
            }
        }

        public async Task<Result<IEnumerable<IngredientsRecips>>> GetIngredientsByRecipeIdAsync(int recipeId)
        {
            if (recipeId <= 0)
            {
                return Result<IEnumerable<IngredientsRecips>>.Failure(
                    Error.Validation("ID da receita inválido.")
                );
            }

            var ingredients = await _recipesRepository.GetIngredientsByRecipeIdAsync(recipeId);

            return Result<IEnumerable<IngredientsRecips>>.Success(ingredients);
        }

        public async Task<Result<IEnumerable<Recipes>>> GetFavoriteRecipesByUserIdAsync(int userId)
        {
            if (userId <= 0)
            {
                return Result<IEnumerable<Recipes>>.Failure(
                    Error.Validation(
                        "ID do utilizador inválido.",
                        new Dictionary<string, string[]> { { nameof(userId), new[] { "Deve ser maior que zero" } } }
                    )
                );
            }

            try
            {
                var favoriteRecipes = await _recipesRepository.GetRecipesWithFavoritesAsync(userId, null);
                return Result<IEnumerable<Recipes>>.Success(favoriteRecipes);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Recipes>>.Failure(
                    Error.InternalServer($"Erro ao obter receitas favoritas: {ex.Message}")
                );
            }
        }
    }
}
