using Contracts.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        public IAccountRepository Accounts { get; }
        public ICategoryRepository Category { get; }
        public ICategoryTypeRepository CategoryType { get; }
        public ICommentsRepository Comments { get; }
        public IDifficultyRepository Difficulty { get; }
        public IIngredientsRepository Ingredients { get; }
        public IIngredientsTypeRepository IngredientsType { get; }
        public IIngredientsRecipsRepository IngredientsRecips { get; }
        public IRatingRepository Rating { get; }
        public IRecipesRepository Recipes { get; }
        public IUserRoleReposiotry UsersRole { get; }
        public IUserSettingsRepository UserSettings { get; }
        public IUsersRepository Users { get; }
        public IFavoritesRepository Favorites { get; }

        public UnitOfWork(IAccountRepository accounts, ICategoryRepository category, ICategoryTypeRepository categoryType, ICommentsRepository comments,
                          IDifficultyRepository dificulty, IIngredientsRepository ingredients, IIngredientsTypeRepository ingredientsType,
                          IIngredientsRecipsRepository ingredientsRecips, IRatingRepository rating, IRecipesRepository recipes,
                          IUserSettingsRepository userSettings, IUsersRepository users, IUserRoleReposiotry userRole, IFavoritesRepository favoritesRepository)
        {
            Accounts = accounts;
            Category = category;
            CategoryType = categoryType;
            Comments = comments;
            Difficulty = dificulty;
            Ingredients = ingredients;
            IngredientsType = ingredientsType;
            IngredientsRecips = ingredientsRecips;
            Rating = rating;
            Recipes = recipes;
            UserSettings = userSettings;
            Users = users;
            UsersRole = userRole;
            Favorites = favoritesRepository;
        }

        public Task<int> CommitAsync()
        {
            return Task.FromResult(1);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
