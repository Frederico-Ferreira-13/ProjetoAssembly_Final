CREATE DATABASE ProjectoAssembly_Final;
GO

USE ProjectoAssembly_Final
GO

CREATE TABLE UsersRole(
	UsersRoleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	RoleName NVARCHAR(100) NOT NULL UNIQUE,
	IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE CategoryType(
		CategoryTypeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		TypeName NVARCHAR(50) NOT NULL UNIQUE,
		IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE IngredientsType(
	IngredientsTypeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	IngredientsTypeName NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE Difficulty(
	DifficultyId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	DifficultyName NVARCHAR(50) NOT NULL UNIQUE,
	IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE Account(
	AccountId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	AccountName NVARCHAR(255) NOT NULL,
	SubscriptionLevel NVARCHAR(50) NOT NULL DEFAULT 'Free',
	IsActive BIT NOT NULL DEFAULT 1,
	CreatorUserId INT NULL
);
GO

CREATE TABLE Users(
	UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	AccountId INT NOT NULL DEFAULT 1,
	UserName NVARCHAR(255) NOT NULL UNIQUE,	
	Email NVARCHAR(255) NOT NULL UNIQUE,
	PasswordHash NVARCHAR(255) NOT NULL,
	Salt NVARCHAR(64) NOT NULL,
	UsersRoleId INT NOT NULL DEFAULT 1,
	IsApproved BIT NOT NULL DEFAULT 0,
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	LastUpdatedAt DATETIME2 NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT FK_Users_Account FOREIGN KEY (AccountId) References Account(AccountId)	
);
GO

CREATE TABLE UserSettings(
	UserSettingId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	UserId INT NOT NULL UNIQUE,
	Theme NVARCHAR(50) NOT NULL,
	Language NVARCHAR(10) NOT NULL,
	ReceiveNotifications BIT NOT NULL DEFAULT 1,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT FK_UserSettings_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

ALTER TABLE Account
ADD CONSTRAINT FK_Account_CreatorUser FOREIGN KEY (CreatorUserId) REFERENCES Users(UserId);
GO

CREATE TABLE Category(
	CategoriesId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	AccountId INT NOT NULL DEFAULT 1,
	CategoryName NVARCHAR(255) NOT NULL,
	ParentCategoryId INT NULL,	
	CategoryTypeId INT NOT NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT FK_Category_Account FOREIGN KEY (AccountId) REFERENCES Account(AccountId),
	CONSTRAINT FK_Category_ParentCategory FOREIGN KEY (ParentCategoryId) REFERENCES Category(CategoriesId), -- FK de autorreferência
	CONSTRAINT FK_Category_CategoryType FOREIGN KEY (CategoryTypeId) REFERENCES CategoryType(CategoryTypeId) -- FK adicionada
);
GO

CREATE TABLE Recipes(
	RecipesId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	UserId INT NOT NULL,
	CategoriesId INT NOT NULL,
	DifficultyId INT NOT NULL,
	Title NVARCHAR(255) NOT NULL,
	Instructions NVARCHAR(MAX) NOT NULL,
	PrepTimeMinutes SMALLINT NOT NULL,
	CookTimeMinutes SMALLINT NOT NULL,
	Servings NVARCHAR(255) NOT NULL,
	ImageUrl NVARCHAR(MAX) NULL,
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	LastUpdatedAt DATETIME2 NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT FK_Recipes_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
	CONSTRAINT FK_Recipes_Category FOREIGN KEY (CategoriesId) REFERENCES Category(CategoriesId),
	CONSTRAINT FK_Recipes_Difficulty FOREIGN KEY (DifficultyId) REFERENCES Difficulty(DifficultyId)
);
GO

CREATE TABLE Ingredients(
	IngredientsId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	IngredientName NVARCHAR(255) NOT NULL UNIQUE,
	IngredientsTypeId INT NOT NULL DEFAULT 1,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT FK_Ingredients_Type FOREIGN KEY (IngredientsTypeId) REFERENCES IngredientsType(IngredientsTypeId)
);
GO

CREATE TABLE IngredientsRecips(
	IngredientsRecipsId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	RecipesId INT NOT NULL,
	IngredientsId INT NOT NULL,
	QuantityValue DECIMAL (10, 2) NOT NULL,
	Unit NVARCHAR(50) NOT NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT UQ_IngredientsRecips UNIQUE (RecipesId, IngredientsId),
	CONSTRAINT FK_IngRecips_Recipes FOREIGN KEY (RecipesId) References Recipes(RecipesId),
	CONSTRAINT FK_IngRecips_Ingredients FOREIGN KEY (IngredientsId) References Ingredients(IngredientsId)
);
GO

CREATE TABLE Ratings(
	RatingsId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	RecipesId INT NOT NULL,
	UserId INT NOT NULL,
	RatingValue INT NOT NULL CHECK (RatingValue BETWEEN 1 AND 5),
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT UQ_Ratings_RecipeUser UNIQUE (RecipesId, UserId),
	CONSTRAINT FK_Ratings_Recipes FOREIGN KEY (RecipesId) References Recipes(RecipesId),
	CONSTRAINT FK_Ratings_Users FOREIGN KEY (UserId) References Users(UserId)
);
GO

CREATE TABLE Comments(
	CommentsId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	RecipesId INT NOT NULL,
	UserId INT NOT NULL,
	CommentText NVARCHAR(500) NOT NULL,
	Rating INT NULL CHECK (Rating BETWEEN 1 AND 5),
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	LastUpdatedAt DATETIME2 NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	IsEdited BIT NOT NULL DEFAULT 0,
	IsDeleted BIT NOT NULL DEFAULT 0,
	OriginalComment NVARCHAR(500) NULL,
	CONSTRAINT FK_Comments_Recipes FOREIGN KEY (RecipesId) References Recipes(RecipesId),
	CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) References Users(UserId)
);
GO

CREATE TABLE FAVORITES (
	FavoritesId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	UserId INT NOT NULL,
	RecipesId INT NOT NULL,
	CreatedAT DATETIME2 NOT NULL DEFAULT GETDATE(),
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT UQ_Favorites_UserRecipes UNIQUE (UserId, RecipesId),
	CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) References Users(UserId),
	CONSTRAINT FK_Favorites_Recipes FOREIGN KEY (RecipesId) REFERENCES Recipes(RecipesId)
);
GO

INSERT INTO Account (AccountName, SubscriptionLevel, IsActive) 
VALUES ('Conta Principal', 'Premium', 1);

INSERT INTO UsersRole (RoleName, IsActive) VALUES ('Admin', 1), ('User', 1);

INSERT INTO Users (
    AccountId, 
    UserName, 
    Email, 
    PasswordHash, 
    Salt, 
    UsersRoleId, 
    IsApproved, 
    IsActive
) 
VALUES (
    1, 
    'FredericoAdmin', 
    'fredericocrf87@hotmail.com', -- O teu email que o sistema vai reconhecer
    'HASH_DA_TUA_PASSWORD',       -- Aqui convém pores o hash gerado pelo teu C#
    'SALT_GERADO', 
    1,                            -- 1 = Role de Admin
    1,                            -- Tu já nasces aprovado!
    1
);
GO