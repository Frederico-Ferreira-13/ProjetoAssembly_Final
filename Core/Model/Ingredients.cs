using Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Ingredients : IEntity
    {
        public int IngredientsId { get; private set; }
        public bool IsActive { get; private set; }

        public string IngredientName { get; protected set; }

        public int IngredientsTypeId { get; protected set; }
        public IngredientsType? Type { get; protected set; }

        private Ingredients(int id, bool isActive, string ingredientName, int ingredientsTypeId)
        {
            if (id <= 0)
            {
                throw new ArgumentException("O ID do Ingrediente é inválido.", nameof(id));
            }

            this.IngredientsId = id;
            this.IsActive = isActive;
            this.IngredientName = ingredientName;
            this.IngredientsTypeId = ingredientsTypeId;
        }

        public static Ingredients Reconstitute(int id, bool isActive, string ingredientName, int ingredientsTypeId)
        {
            return new Ingredients(id, isActive, ingredientName, ingredientsTypeId);
        }

        public Ingredients([NotNull] string ingredientName, int ingredientsTypeId)
        {
            ValidateIngredients(ingredientName);

            if (ingredientsTypeId <= 0)
            {
                throw new ArgumentException("O ID do Tipo de Ingrediente é obrigatório e válido.", nameof(ingredientsTypeId));
            }

            this.IngredientsId = default;
            this.IsActive = true;
            this.IngredientName = ingredientName;
            this.IngredientsTypeId = ingredientsTypeId;
        }

        public void UpdateDetails(string newIngredientName, int newIngredientsTypeId)
        {

            ValidateIngredients(newIngredientName);

            if (IngredientName != newIngredientName)
            {
                IngredientName = newIngredientName;
            }

            if (IngredientsTypeId != newIngredientsTypeId)
            {
                if (newIngredientsTypeId <= 0)
                {
                    throw new ArgumentException("O novo ID do Tipo de Ingrediente é inválido.", nameof(newIngredientsTypeId));
                }
                IngredientsTypeId = newIngredientsTypeId;
            }
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
            }
        }

        private void ValidateIngredients([NotNull] string? ingredientName)
        {
            if (string.IsNullOrWhiteSpace(ingredientName))
            {
                throw new ArgumentException("O nome do ingrediente é obrigatório.");
            }
        }

        public int GetId() => IngredientsId;

        public void SetId(int id)
        {
            if (IngredientsId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            IngredientsId = id;
        }

        public bool GetIsActive() => IsActive;
    }
}
