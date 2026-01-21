using Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Recipes : IEntity
    {
        public int RecipesId { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsApproved { get; private set; }

        public int UserId { get; protected set; }
        public int CategoriesId { get; protected set; }

        public int DifficultyId { get; protected set; }

        public string Title { get; protected set; }
        public string Instructions { get; protected set; }
        public int PrepTimeMinutes { get; protected set; }
        public int CookTimeMinutes { get; protected set; }
        public string Servings { get; protected set; }
        public string? ImageUrl { get; protected set; }

        public int FavoriteCount { get; set; } = 0;
        public bool IsFavorite { get; set; } = false;

        public DateTime CreatedAt { get; protected set; }
        public DateTime? LastUpdatedAt { get; protected set; }

        public Difficulty? Difficulty { get; protected set; }

        public virtual ICollection<IngredientsRecips> Ingredients { get; protected set; } = new List<IngredientsRecips>();

        private const int MinTitleLength = 5;
        private const int MinInstructionsLength = 20;

        [SetsRequiredMembers]
        private Recipes()
        {
            this.RecipesId = default;
            this.IsActive = true;
            Title = string.Empty;
            Instructions = string.Empty;
            Servings = string.Empty;
        }

        public Recipes(int userId, int categoriesId, int difficultyId, string title, string instructions,
                        int prepTimeMinutes, int cookTimeMinutes, string servings, bool isApproved = false)
        {
            ValidateRecipe(userId, categoriesId, difficultyId, title, instructions, prepTimeMinutes,
                cookTimeMinutes, servings);

            this.RecipesId = default;
            this.IsActive = true;
            this.IsApproved = isApproved;

            UserId = userId;
            CategoriesId = categoriesId;
            DifficultyId = difficultyId;
            Title = title;
            Instructions = instructions;
            PrepTimeMinutes = prepTimeMinutes;
            CookTimeMinutes = cookTimeMinutes;
            Servings = servings;

            this.CreatedAt = DateTime.UtcNow;
            this.LastUpdatedAt = null;
        }

        private Recipes(int id, bool isActive, int userId, int categoriesId, int difficultyId, string title,
            string instructions, int prepTimeMinutes, int cookTimeMinutes, string servings,
            DateTime createdAt, DateTime? lastUpdatedAt)
        {
            this.RecipesId = id;
            this.IsActive = isActive;

            UserId = userId;
            CategoriesId = categoriesId;
            DifficultyId = difficultyId;
            Title = title;
            Instructions = instructions;
            PrepTimeMinutes = prepTimeMinutes;
            CookTimeMinutes = cookTimeMinutes;
            Servings = servings;

            this.CreatedAt = createdAt;
            this.LastUpdatedAt = lastUpdatedAt;
        }

        public static Recipes Reconstitute(int id, int userId, int categoriesId, int difficultyId, string title, string instructions,
                                         int prepTimeMinutes, int cookTimeMinutes, string servings,
                                         DateTime createdAt, DateTime? lastUpdatedAt, bool isActive)
        {            
            var recipe = new Recipes(id, isActive, userId, categoriesId, difficultyId, title, instructions, prepTimeMinutes, cookTimeMinutes, servings,
                               createdAt, lastUpdatedAt);

            return recipe;
        }

        public void ChangeDifficulty(int newDifficultyId)
        {
            if (newDifficultyId <= 0)
            {
                throw new ArgumentException("O ID da nova dificuldade deve ser positivo.", nameof(newDifficultyId));
            }
            if (DifficultyId != newDifficultyId)
            {
                DifficultyId = newDifficultyId;
                SetLastUpdatedAt();
            }
        }

        private void SetLastUpdatedAt()
        {
            this.LastUpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                SetLastUpdatedAt();
            }
        }

        public void UpdateDetails(string newTitle, string newInstructions, int newPrepTime, int newCookTime, string newServings)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar uma receita inativa.");
            }

            ValidateTextProperties(newTitle, newInstructions, newServings);
            ValidateTimeProperties(newPrepTime, newCookTime);

            bool changed = false;

            if (Title != newTitle)
            {
                Title = newTitle;
                changed = true;
            }
            if (Instructions != newInstructions)
            {
                Instructions = newInstructions;
                changed = true;
            }
            if (PrepTimeMinutes != newPrepTime)
            {
                PrepTimeMinutes = newPrepTime;
                changed = true;
            }
            if (CookTimeMinutes != newCookTime)
            {
                CookTimeMinutes = newCookTime;
                changed = true;
            }
            if (Servings != newServings)
            {
                Servings = newServings;
                changed = true;
            }

            if (changed)
            {
                SetLastUpdatedAt();
            }
        }

        public void Approve()
        {
            if (!IsApproved)
            {
                IsApproved = true;
                SetLastUpdatedAt();
            }
        }

        public void SetImageUrl(string url)
        {
            this.ImageUrl = url;
            SetLastUpdatedAt();
        }

        public void ChangeCategory(int newCategoriesId)
        {
            if (newCategoriesId <= 0)
            {
                throw new ArgumentException("O ID da nova categoria deve ser positivo.", nameof(newCategoriesId));
            }

            if (CategoriesId != newCategoriesId)
            {
                CategoriesId = newCategoriesId;
                SetLastUpdatedAt();
            }
        }

        private static void ValidateRecipe(int userId, int categoriesId, int difficultyId, string title, string instructions,
            int prepTimeMinutes, int cookTimeMinutes, string servings)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("O ID do Autor deve ser positivo.", nameof(userId));
            }
            if (categoriesId <= 0)
            {
                throw new ArgumentException("O ID da Categoria deve ser positvo.", nameof(categoriesId));
            }
            if (difficultyId <= 0)
            {
                throw new ArgumentException("O ID da Dificuldade deve ser positivo.", nameof(difficultyId));
            }

            ValidateTextProperties(title, instructions, servings);
            ValidateTimeProperties(prepTimeMinutes, cookTimeMinutes);
        }

        private static void ValidateTextProperties([NotNull] string? title, [NotNull] string? instructions,
            [NotNull] string servings)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Length < MinTitleLength)
            {
                throw new ArgumentException($"O título é obrigatório e deve ter pelo menos {MinTitleLength} caracteres.", nameof(title));
            }
            if (string.IsNullOrWhiteSpace(instructions) || instructions.Length < MinInstructionsLength)
            {
                throw new ArgumentException($"As instruções são obrigatórias e devem ter pelo menos {MinInstructionsLength} caracteres.", nameof(instructions));
            }
            if (string.IsNullOrWhiteSpace(servings))
            {
                throw new ArgumentException("A descrição de Proções (Servings) é obrigatória.", nameof(servings));
            }
        }

        private static void ValidateTimeProperties(int prepTimeMinutes, int cookTimeMinutes)
        {
            if (prepTimeMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prepTimeMinutes), "O tempo de preparação não pode ser negativo.");
            }
            if (cookTimeMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cookTimeMinutes), "O tempo de cozedura não pode ser negativo.");
            }
            if (prepTimeMinutes + cookTimeMinutes == 0)
            {
                throw new ArgumentException("O tempo total (preparação + cozedura) de ser maior que zero.");
            }
        }

        public bool IsDeleted => !IsActive;

        public int GetId() => RecipesId;

        public void SetId(int id)
        {
            if (RecipesId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            RecipesId = id;
        }

        public bool GetIsActive() => IsActive;
    }
}
