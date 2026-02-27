using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Contracts.Repository;
using Contracts.Service;
using Repo.Repository;
using Service.Services;
using Core.Common;
using Core.Model.ValueObjects;

namespace IDContainer
{
    public static class ServiceCollectionExtensions
    {
        
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {

            services.AddScoped<IRecipesService, RecipeService>();
            services.AddScoped<IIngredientsService, IngredientService>();
            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IRatingService, RatingService>();
            services.AddScoped<ICommentsService, CommentsService>();
            services.AddScoped<IDifficultyService, DifficultyService>();
            services.AddScoped<IFavoritesService, FavoritesService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IRecipesRepository, RecipesRepository>();
            services.AddScoped<IIngredientsRepository, IngredientsRepository>();
            services.AddScoped<IUsersRepository, UsersRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryTypeRepository, CategoryTypeRepository>();
            services.AddScoped<IIngredientsRecipsRepository, IngredientsRecipsRepository>();
            services.AddScoped<IIngredientsTypeRepository, IngredientsTypeRepository>();
            services.AddScoped<IRatingRepository, RatingsRepository>();
            services.AddScoped<ICommentsRepository, CommentsRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IDifficultyRepository, DifficultyRepository>();
            services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
            services.AddScoped<IFavoritesRepository, FavoritesRepository>();

            services.AddScoped<CloudService>();

            return services;
        }

        public static IServiceCollection AddAuthenticationConfig(this IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/AccessDenied";
                    options.Cookie.Name = "ReceitasFredericoAuth";
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                });

            return services;
        }

        public static IServiceCollection AddAppSettingsConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            return services;
        }
    }
}
