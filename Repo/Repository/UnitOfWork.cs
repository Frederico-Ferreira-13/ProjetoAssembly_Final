using Contracts.Repository;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using Microsoft.Extensions.Configuration;

namespace Repo.Repository
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private TransactionScope? _scope;
        private bool _disposed = false;
        private readonly IConfiguration _configuration;

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

        public UnitOfWork(IConfiguration configuration, IAccountRepository accounts, ICategoryRepository category, ICategoryTypeRepository categoryType, ICommentsRepository comments,
                          IDifficultyRepository dificulty, IIngredientsRepository ingredients, IIngredientsTypeRepository ingredientsType,
                          IIngredientsRecipsRepository ingredientsRecips, IRatingRepository rating, IRecipesRepository recipes,
                          IUserSettingsRepository userSettings, IUsersRepository users, IUserRoleReposiotry userRole, IFavoritesRepository favoritesRepository)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

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

        public async Task BeginTransactionAsync()
        {
            if (_scope != null)
            {
                throw new InvalidOperationException("Já existe uma transação ativa.");
            }

            var options = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted, // Padrão bom para leitura/escrita consistente
                Timeout = TimeSpan.FromSeconds(30) // Ajustar se necessitar de mais tempo
            };

            _scope = new TransactionScope(
                TransactionScopeOption.Required,
                options,
                TransactionScopeAsyncFlowOption.Enabled //Para Async
            );

            await Task.CompletedTask;
        }

        public async Task<int> CommitAsync()
        {
            if(_scope == null)
            {
                return 0;
            }

            _scope.Complete();
            await Task.CompletedTask;
            return 1;
        }

        public void Rollback()
        {
            _scope?.Dispose();
            _scope = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _scope?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
