using Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class IngredientsRecips : IEntity
    {
        public int IngredientsRecipsId { get; private set; }       

        public int RecipesId { get; protected set; }
        public int IngredientsId { get; protected set; }

        public decimal QuantityValue { get; protected set; }
        public string Unit { get; protected set; } = string.Empty;
        public string Detail { get; protected set; } = string.Empty;

        public virtual Ingredients? Ingredient { get; protected set; }

        private IngredientsRecips()
        {
            this.IngredientsRecipsId = default;
        }

        public IngredientsRecips(int recipesId, int ingredientsId, decimal quantityValue, [NotNull] string unit)
        {
            Validate(recipesId, ingredientsId, quantityValue, unit);

            IngredientsRecipsId = default;

            RecipesId = recipesId;
            IngredientsId = ingredientsId;
            QuantityValue = quantityValue;
            Unit = unit;
        }

        private IngredientsRecips(int id, int recipesId, int ingredientsId, decimal quantityValue,
             string unit)
        {
            IngredientsRecipsId = id;            

            RecipesId = recipesId;
            IngredientsId = ingredientsId;
            QuantityValue = quantityValue;
            Unit = unit;
        }

        public static IngredientsRecips Reconstitute(int id, int recipesId, int ingredientsId,
            decimal quantityValue, string unit)
        {
            return new IngredientsRecips(id, recipesId, ingredientsId, quantityValue, unit);
        }

        public void Update(decimal newQuantityValue, [NotNull] string newUnit, string? newDetail)
        {           

            ValidateQuantityAndUnit(newQuantityValue, newUnit);

            if (QuantityValue != newQuantityValue || !Unit.Equals(newUnit, StringComparison.OrdinalIgnoreCase))
            {
                QuantityValue = newQuantityValue;
                Unit = newUnit;
                Detail = newDetail;
            }
        }      

        private static void Validate(int recipesId, int ingredientsId, decimal quantityValue, [NotNull] string? unit)
        {
            if (recipesId <= 0)
            {
                throw new ArgumentException("ID da Receita inválido.", nameof(recipesId));
            }

            if (ingredientsId <= 0)
            {
                throw new ArgumentException("ID do Ingrediente inválido.", nameof(ingredientsId));
            }

            ValidateQuantityAndUnit(quantityValue, unit);
        }

        private static void ValidateQuantityAndUnit(decimal quantityValue, [NotNull] string? unit)
        {
            if (quantityValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantityValue), "A quantidade deve ser positiva.");
            }
            if (string.IsNullOrWhiteSpace(unit))
            {
                throw new ArgumentException("A Unidade é obrigatória.", nameof(unit));
            }

            if (unit.Length > 50)
            {
                throw new ArgumentException("A Unidade não pode exceder 50 caracteres.", nameof(unit));
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(unit, @"^[a-zA-Z0-9\s\p{P}]+$"))
            {
                throw new ArgumentException("A Unidade contém caracteres inválidos.", nameof(unit));
            }
        }

        public int GetId() => IngredientsRecipsId;

        public void SetId(int id)
        {
            if (IngredientsRecipsId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            IngredientsRecipsId = id;
        }

        public bool GetIsActive() => true;
    }
}
