USE master;
GO

CREATE DATABASE ProjectoAssembly_Final;
GO

USE ProjectoAssembly_Final;
GO

CREATE TABLE UsersRole(
	UsersRoleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, 
	RoleName NVARCHAR(100) NOT NULL UNIQUE,
	IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE CategoryType(
	CategoryTypeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	TypeName NVARCHAR(50) NOT NULL UNIQUE,
	IsActive BIT NOT NULL DEFAULT 1
);

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

CREATE TABLE ACCOUNT(
	AccountId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	AccountName NVARCHAR(255) NOT NULL,
	SubscriptionLevel NVARCHAR(50) NOT NULL DEFAULT 'Free',
	IsActive BIT NOT NULL DEFAULT 1,
	CreatorUserId INT NULL
);

CREATE TABLE Users(
	UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	AccountId INT NOT NULL DEFAULT 1,
	UserName NVARCHAR(255) NOT NULL UNIQUE,
	Email NVARCHAR(255) NOT NULL UNIQUE,
	PasswordHash NVARCHAR (255) NOT NULL,
	Salt NVARCHAR(64) NOT NULL,
	UsersRoleId INT NOT NULL DEFAULT 1,
	IsApproved BIT NOT NULL DEFAULT 0,
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	LastUpdatedAt DATETIME2 NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT FK_Users_Account FOREIGN KEY (AccountId) REFERENCES Account(AccountId),
	CONSTRAINT FK_Users_UserRole FOREIGN KEY (UsersRoleId) REFERENCES UsersRole(UsersRoleId)
);

CREATE TABLE UserSettings (
    UserSettingId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Theme NVARCHAR(50) DEFAULT 'Light',
	Language NVARCHAR(10) DEFAULT 'pt-PT',
    ReceiveNotifications BIT DEFAULT 1,
	IsActive BIT NOT NULL DEFAULT 1,
	LastUpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_UserSettings_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

ALTER TABLE Account
ADD CONSTRAINT FK_Account_CreatorUser FOREIGN KEY (CreatorUserId) REFERENCES Users(UserId);

CREATE TABLE Category(
	CategoriesId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	AccountId INT NOT NULL DEFAULT 1,
	CategoryName NVARCHAR(255) NOT NULL,
	ParentCategoryId INT NULL,
	CategoryTypeId INT NOT NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT FK_Category_Account FOREIGN KEY (AccountId) REFERENCES Account(AccountId),
	CONSTRAINT FK_Category_ParentCategory FOREIGN KEY (ParentCategoryId) REFERENCES Category(CategoriesId),
	CONSTRAINT FK_Category_CategoryType FOREIGN KEY (CategoryTypeId) REFERENCES CategoryType(CategoryTypeId)
);

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

CREATE TABLE Favorites (
	FavoritesId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	UserId INT NOT NULL,
	RecipesId INT NOT NULL,
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT UQ_Favorites_UserRecipes UNIQUE (UserId, RecipesId),
	CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
	CONSTRAINT FK_Favorites_Recipes FOREIGN KEY (RecipesId) REFERENCES Recipes(RecipesId)
);

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

INSERT INTO UsersRole (RoleName) VALUES ('Admin'), ('User');
INSERT INTO Account (AccountName) VALUES ('Sistema');
INSERT INTO Difficulty (DifficultyName) VALUES ('Fácil'), ('Média'), ('Difícil');
INSERT INTO CategoryType (TypeName) VALUES ('Comida'), ('Bebida');
INSERT INTO Category (CategoryName, CategoryTypeId) VALUES ('Sopas', 1), ('Carne', 1), ('Peixe', 1), ('Sobremesas', 1);
GO

DELETE FROM Recipes;
DELETE FROM Category;
GO

SET IDENTITY_INSERT Category ON;
INSERT INTO Category (CategoriesId, AccountId, CategoryName, CategoryTypeId, IsActive) VALUES 
(1, 1, 'Sopas', 1, 1),
(2, 1, 'Carne', 1, 1),
(3, 1, 'Peixe', 1, 1),
(4, 1, 'Sobremesas', 1, 1);
SET IDENTITY_INSERT Category OFF;
GO

INSERT INTO Recipes (UserId, CategoriesId, DifficultyId, Title, Instructions, PrepTimeMinutes, CookTimeMinutes, Servings, ImageUrl, IsActive)
VALUES 
(1, 1, 1, 'Sopa de Legumes da Avó', 'Cozar tudo e triturar.', 15, 30, '4 Pessoas', 'sopa.jpg', 1),
(1, 2, 2, 'Arroz de Pato Especial', 'Desfiar o pato e levar ao forno.', 20, 45, '6 Pessoas', 'arroz-de-pato.jpg', 1),
(1, 3, 2, 'Bacalhau à Brás', 'Desfiar o bacalhau e misturar com batata palha.', 15, 15, '2 Pessoas', 'bacalhau.jpg', 1),
(1, 4, 1, 'Arroz Doce Cremoso', 'Cozinho com leite e canela.', 10, 40, '8 Pessoas', 'arroz-doce.jpg', 1);
GO