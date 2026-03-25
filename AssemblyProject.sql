CREATE DATABASE ProjectoAssembly_Final;
GO

USE ProjectoAssembly_Final;
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
	Name NVARCHAR(255) NOT NULL,
	UserName NVARCHAR(255) NOT NULL UNIQUE,
	Email NVARCHAR(255) NOT NULL UNIQUE,
	ProfilePicture NVARCHAR(MAX) NULL,
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
GO

ALTER TABLE Account
ADD CONSTRAINT FK_Account_CreatorUser FOREIGN KEY (CreatorUserId) REFERENCES Users(UserId);

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
	IsApproved BIT NOT NULL DEFAULT 0,
	AverageRating FLOAT NOT NULL DEFAULT 0,
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
	Detail NVARCHAR(255) NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT UQ_IngredientsRecips UNIQUE (RecipesId, IngredientsId),
	CONSTRAINT FK_IngRecips_Recipes FOREIGN KEY (RecipesId) References Recipes(RecipesId),
	CONSTRAINT FK_IngRecips_Ingredients FOREIGN KEY (IngredientsId) References Ingredients(IngredientsId)
);
GO

CREATE TABLE Favorites(
	FavoritesId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	UserId INT NOT NULL,
	RecipesId INT NOT NULL,
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	IsActive BIT NOT NULL DEFAULT 1,
	CONSTRAINT UQ_Favorites_UserRecipes UNIQUE (UserId, RecipesId),
	CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
	CONSTRAINT FK_Favorites_Recipes FOREIGN KEY (RecipesId) REFERENCES Recipes(RecipesId)
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
	Rating INT NULL DEFAULT 0 CHECK (Rating BETWEEN 0 AND 5),
	CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
	LastUpdatedAt DATETIME2 NULL,
	IsActive BIT NOT NULL DEFAULT 1,
	IsEdited BIT NOT NULL DEFAULT 0,
	IsDeleted BIT NOT NULL DEFAULT 0,
	OriginalComment NVARCHAR(500) NULL,
	ParentCommentId INT NULL,
	CONSTRAINT FK_Comments_Recipes FOREIGN KEY (RecipesId) References Recipes(RecipesId),
	CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) References Users(UserId),
	CONSTRAINT FK_Comments_Parent FOREIGN KEY (ParentCommentId) REFERENCES Comments(CommentsId)
);
GO

INSERT INTO UsersRole (RoleName, IsActive) VALUES ('Admin', 1), ('User', 1);
GO

INSERT INTO Account (AccountName, SubscriptionLevel, IsActive) VALUES ('Sistema', 'Free', 1);
GO

INSERT INTO Difficulty (DifficultyName, IsActive) VALUES ('Fácil', 1), ('Média', 1), ('Difícil', 1);
GO

INSERT INTO CategoryType (TypeName, IsActive) VALUES ('Comida', 1), ('Bebida', 1);
GO

INSERT INTO IngredientsType (IngredientsTypeName) VALUES ('Geral'), ('Mercearia'), ('Frescos'), ('Especiarias'), ('Laticínios');
GO

SET IDENTITY_INSERT Category ON;
INSERT INTO Category (CategoriesId, AccountId, CategoryName, CategoryTypeId)
VALUES
    (1, 1, 'Sopas', 1),
    (2, 1, 'Carne', 1),
    (3, 1, 'Peixe', 1),
	(4, 1, 'Vegetariano', 1),
    (5, 1, 'Sobremesas', 1);
SET IDENTITY_INSERT Category OFF;
GO

INSERT INTO Users (AccountId, Name, UserName, Email, PasswordHash, Salt, UsersRoleId, IsApproved)
VALUES (1, 'Frederico Ferreira', 'Frederico_Admin', 'fredericocrf87@hotmail.com', '$2a$12$8MCSOHsi2CXqd7qMY/Hiv.MK/KY9sjTIL4mzAQDe3Bz4dlGTK2dgm', 'SALT_ADMIN', 1, 1);

INSERT INTO Users (AccountId, Name, UserName, Email, PasswordHash, Salt, UsersRoleId, IsApproved)
VALUES (1, 'User Teste', 'UserTeste', 'teste@fredericoreceitas.pt', '$2a$12$tecgzT9tqeVR6iWmz2bM.udA0LWNvWhBnniKt.C/3SeI.PO/he5kC', 'SALT_TESTE', 2, 1);

INSERT INTO Recipes (UserId, CategoriesId, DifficultyId, Title, Instructions, PrepTimeMinutes, CookTimeMinutes, Servings, ImageUrl, IsApproved)
VALUES
    (1, 4, 1, 'Arroz Doce Cremoso', 'Cozinha o arroz em leite com canela.', 10, 45, '6 pessoas', 'arroz-doce.jpg', 1),
    (2, 2, 2, 'Arroz de Pato Tradicional', 'Coze o pato e leva ao forno com arroz.', 30, 90, '4 pessoas', 'arroz-de-pato.jpg', 1),
    (1, 1, 1, 'Sopa de Legumes Caseira', 'Tritura os legumes cozidos.', 15, 35, '6 pessoas', 'sopa.jpg', 1),
    (1, 3, 2, 'Bacalhau à Brás', 'Refoga bacalhau com batata palha e ovos.', 20, 25, '4 pessoas', 'bacalhau.jpg', 1),
    (2, 5, 3, 'Bolo de Chocolate Vegan', 'Mistura farinha, cacau e leite vegetal.', 15, 35, '10 fatias', 'bolo-de-chocolate-vegan.jpg', 1);
GO

UPDATE Recipes SET IsApproved = 0 WHERE RecipesId = 5;

PRINT '=== TODOS OS DADOS INICIAIS INSERIDOS COM SUCESSO ===';

SELECT 'Utilizadores' AS Tabela, UserId, UserName, Email, UsersRoleId FROM Users ORDER BY UserId;
SELECT 'Contas' AS Tabela, * FROM Account;
SELECT 'Categorias' AS Tabela, * FROM Category ORDER BY CategoriesId;
SELECT 'Receitas' AS Tabela, RecipesId, Title, UserId, IsApproved FROM Recipes ORDER BY RecipesId;
GO

UPDATE Recipes SET CategoriesId = 4 WHERE Title LIKE '%Arroz Doce%'

UPDATE Recipes 
SET CategoriesId = 5 
WHERE Title = 'Bolo de Chocolate Vegan';


UPDATE Recipes 
SET CategoriesId = 5 
WHERE Title = 'Bolo de Chocolate Vegan';

UPDATE Recipes 
SET CategoriesId = 4 
WHERE Title = 'Arroz Doce Cremoso';

SELECT Title, CategoriesId FROM Recipes WHERE Title IN ('Bolo de Chocolate Vegan', 'Arroz Doce Cremoso');

SELECT RecipesId, Title, IsActive FROM Recipes WHERE RecipesId = 1;

SELECT * FROM IngredientsRecips

SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%Ingredients%';

SELECT ir.*, i.IngredientName 
FROM IngredientsRecips ir
INNER JOIN Ingredients i ON ir.IngredientsId = i.IngredientsId
WHERE ir.RecipesId = 1;

SELECT * FROM Recipes WHERE RecipesId = 1;
SELECT * FROM IngredientsRecips WHERE RecipesId = 1;
SELECT * FROM Comments WHERE RecipesId = 1;

SELECT RecipesId, Title, CategoriesId, DifficultyId 
FROM Recipes 
ORDER BY RecipesId;

SELECT * FROM Recipes WHERE RecipesId = 1;
SELECT ir.*, i.IngredientName 
FROM IngredientsRecips ir
INNER JOIN Ingredients i ON ir.IngredientsId = i.IngredientsId
WHERE ir.RecipesId = 1;

SELECT RecipesId, Title, PrepTimeMinutes FROM Recipes WHERE RecipesId = 1;
SELECT * FROM IngredientsRecips WHERE RecipesId = 1;

INSERT INTO IngredientsType (IngredientsTypeName) 
VALUES ('Geral'), ('Mercearia'), ('Frescos'), ('Especiarias'), ('Laticínios');
GO

SELECT * FROM IngredientsType;

SELECT * FROM IngredientsRecips;

Select * FROM Recipes

SELECT RecipesId, PrepTimeMinutes, CookTimeMinutes FROM Recipes WHERE RecipesId;
SELECT ir.*, i.IngredientName 
FROM IngredientsRecips ir
INNER JOIN Ingredients i ON ir.IngredientsId = i.IngredientsId
WHERE ir.RecipesId;

SELECT * FROM Recipes 
WHERE IsActive = 1 
  AND (Title LIKE @Search OR @Search IS NULL)
  AND (CategoriesId = @CatId OR @CatId IS NULL)

  SELECT * FROM Favorites

  SELECT * FROM Favorites WHERE UserId = 1

  SELECT * FROM Users
  
SELECT 
    r.RecipesId, 
    r.Title, 
    r.UserId, 
    r.CategoriesId, 
    r.DifficultyId, 
    r.PrepTimeMinutes, 
    r.CookTimeMinutes, 
    r.Servings, 
    r.ImageUrl, 
    r.IsApproved, 
    r.IsActive,
    ISNULL(AVG(CAST(rt.RatingValue AS FLOAT)), 0) as AverageRating,
    COUNT(DISTINCT f.FavoritesId) as FavoriteCount
FROM Recipes r
LEFT JOIN Ratings rt ON r.RecipesId = rt.RecipesId AND rt.IsActive = 1
LEFT JOIN Favorites f ON r.RecipesId = f.RecipesId AND f.IsActive = 1
WHERE r.IsActive = 1 -- Opcional: apenas receitas ativas
GROUP BY 
    r.RecipesId, r.Title, r.UserId, r.CategoriesId, r.DifficultyId, 
    r.PrepTimeMinutes, r.CookTimeMinutes, r.Servings, r.ImageUrl, 
    r.IsApproved, r.IsActive;

	SELECT * FROM Ratings

	select * from IngredientsRecips

	ALTER TABLE Recipes ADD AverageRating FLOAT NOT NULL DEFAULT 0;