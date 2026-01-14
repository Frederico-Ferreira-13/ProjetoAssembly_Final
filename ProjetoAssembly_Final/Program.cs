using Service.Services;
using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using Repo.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

SQL.Initialize(connectionString);

builder.Services.AddControllers();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSwaggerGen();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null)
{
    throw new Exception("Erro Crítico: Secção 'JwtSettings' não encontrada no appsettings.json");
}
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRecipesRepository, RecipesRepository>();
builder.Services.AddScoped<IIngredientsRepository, IngredientsRepository>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryTypeRepository, CategoryTypeRepository>();
builder.Services.AddScoped<IIngredientsRecipsRepository, IngredientsRecipsRepository>();
builder.Services.AddScoped<IIngredientsTypeRepository, IngredientsTypeRepository>();
builder.Services.AddScoped<IRatingRepository, RatingsRepository>();
builder.Services.AddScoped<ICommentsRepository, CommentsRepository>();
builder.Services.AddScoped<IUserRoleReposiotry, UserRoleRepository>();
builder.Services.AddScoped<IDifficultyRepository, DifficultyRepository>();
builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
builder.Services.AddScoped<IFavoritesRepository, FavoritesRepository>();

builder.Services.AddScoped<IRecipesService, RecipeService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IIngredientsService, IngredientService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryTypeService, CategoryTypeService>();
builder.Services.AddScoped<IIngredientsRecipesService, IngredientsRecipsService>();
builder.Services.AddScoped<IIngredientsTypeService, IngredientsTypeService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<ICommentsService, CommentsService>();
builder.Services.AddScoped<IUsersRoleService, UsersRoleService>();
builder.Services.AddScoped<IDifficultyService, DifficultyService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/AccessDenied";
        options.Cookie.Name = "ReceitasFredericoAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapFallbackToFile("Index.html");

app.Run();