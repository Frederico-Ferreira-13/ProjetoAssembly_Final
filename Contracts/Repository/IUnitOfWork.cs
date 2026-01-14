using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        ICategoryRepository Category { get; }
        ICategoryTypeRepository CategoryType { get; }
        ICommentsRepository Comments { get; }
        IDifficultyRepository Difficulty { get; }
        IIngredientsRepository Ingredients { get; }
        IIngredientsTypeRepository IngredientsType { get; }
        IIngredientsRecipsRepository IngredientsRecips { get; }
        IRatingRepository Rating { get; }
        IRecipesRepository Recipes { get; }
        IUserRoleReposiotry UsersRole { get; }
        IUserSettingsRepository UserSettings { get; }
        IUsersRepository Users { get; }
        IFavoritesRepository Favorites { get; }

        Task<int> CommitAsync();
    }
}
