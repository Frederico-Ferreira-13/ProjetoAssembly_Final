using Core.Common;
using Core.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Ratings : IEntity
    {
        public int RatingsId { get; private set; }
        public bool IsActive { get; private set; }

        public int RecipesId { get; protected set; }
        public int UserId { get; protected set; }
        public StarRating RatingValue { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        private const int MinRating = 1;
        private const int MaxRating = 5;

        [SetsRequiredMembers]
        private Ratings()
        {
            this.RatingsId = default;
            this.IsActive = true;
        }

        public Ratings(int recipesId, int userId, int ratingValue)
        {
            ValidateRating(recipesId, userId);

            this.RatingsId = default;
            this.IsActive = true;

            RecipesId = recipesId;
            UserId = userId;
            RatingValue = StarRating.Create(ratingValue);
            this.CreatedAt = DateTime.UtcNow;
        }

        private Ratings(int id, DateTime createdAt, bool isActive,
            int recipesId, int userId, StarRating ratingValue)
        {
            this.RatingsId = id;
            this.IsActive = isActive;

            RecipesId = recipesId;
            UserId = userId;
            RatingValue = ratingValue;
            this.CreatedAt = createdAt;
        }

        public static Ratings Reconstitute(int id, DateTime createdAt, bool isActive, int recipesId,
            int userId, StarRating ratingValue)
        {
            return new Ratings(id, createdAt, isActive, recipesId, userId, ratingValue);
        }

        public void UpdateRating(int newRatingValue)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Não é possível atualizar uma avaliação inativa.");
            }

            ValidateRatingValue(newRatingValue);

            var newRating = StarRating.Create(newRatingValue);

            if (!RatingValue.Equals(newRating))
            {
                RatingValue = newRating;
            }
        }

        private static void ValidateRating(int recipesId, int userId)
        {
            if (recipesId <= 0)
            {
                throw new ArgumentException("O ID da Receita deve ser positivo.", nameof(recipesId));
            }
            if (userId <= 0)
            {
                throw new ArgumentException("O ID do Utilizador deve ser positivo.", nameof(userId));
            }
        }

        private static void ValidateRatingValue(int ratingValue)
        {
            if (ratingValue < MinRating || ratingValue > MaxRating)
            {
                throw new ArgumentOutOfRangeException(nameof(ratingValue),
                    $"A avaliação deve estar entre {MinRating} e {MaxRating} estrelas.");
            }
        }

        public int GetId() => RatingsId;

        public void SetId(int id)
        {
            if (RatingsId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            RatingsId = id;
        }

        public bool GetIsActive() => IsActive;
    }
}
