using Core.Common;
using Core.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contracts.Service
{
    public interface IIngredientsService
    {
        Task<Result<Ingredients>> GetIngredientByIdAsync(int ingredientId);
        Task<Result<IEnumerable<Ingredients>>> SearchIngredientsAsync(string searchIngredient);
        Task<Result<Ingredients>> CreateIngredientAsync(Ingredients newIngredient);
        Task<Result> UpdateIngredientAsync(Ingredients ingredientToUpdate);
        Task<Result> DeleteIngredientAsync(int ingredientId);

        Task<Result<IngredientsType>> GetIngredientsTypeByIdAsync(int id);
        Task<Result<IngredientsType>> GetIngredientsTypeByNameAsync(string name);
        Task<Result<IEnumerable<IngredientsType>>> GetAllIngredientsTypesAsync();
        Task<Result<IngredientsType>> CreateIngredientsTypeAsync(IngredientsType ingredientsType);
        Task<Result> UpdateIngredientsTypeAsync(IngredientsType updateIngredientsType);
        Task<Result> DeleteIngredientsTypeAsync(int id);

        Task<Result<IngredientsRecips>> GetIngredientsRecipsByIdAsync(int id);
        Task<Result<IEnumerable<IngredientsRecips>>> GetIngredientsByRecipeIdAsync(int recipeId);
        Task<Result<IngredientsRecips>> AddIngredientToRecipeAsync(IngredientsRecips newIngredientsRecipes);
        Task<Result> UpdateIngredientsInRecipeAsync(int id, IngredientsRecips ingredientsRecipesToUpdate);
        Task<Result> RemoveIngredientFromRecipeAsync(int id);
    }
}
