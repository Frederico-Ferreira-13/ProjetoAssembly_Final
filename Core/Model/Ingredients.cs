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

        public string IngredientName { get; protected set; }

        public int IngredientsTypeId { get; protected set; }
        public IngredientsType? Type { get; protected set; }

        private Ingredients(int id, string ingredientName, int ingredientsTypeId)
        {
            if (id <= 0)
            {
                throw new ArgumentException("O ID do Ingrediente é inválido.", nameof(id));
            }

            IngredientsId = id;            
            IngredientName = ingredientName;
            IngredientsTypeId = ingredientsTypeId;
        }

        public static Ingredients Reconstitute(int id, string ingredientName, int ingredientsTypeId)
        {
            return new Ingredients(id, ingredientName, ingredientsTypeId);
        }

        public Ingredients([NotNull] string ingredientName, int ingredientsTypeId)
        {
            ValidateIngredients(ingredientName);

            if (ingredientsTypeId <= 0)
            {
                throw new ArgumentException("O ID do Tipo de Ingrediente é obrigatório e válido.", nameof(ingredientsTypeId));
            }

            IngredientsId = default;            
            IngredientName = ingredientName;
            IngredientsTypeId = ingredientsTypeId;
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

        public bool GetIsActive() => true;
    }
}
