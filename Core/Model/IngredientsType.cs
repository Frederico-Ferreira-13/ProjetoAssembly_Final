using Core.Common;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class IngredientsType : IEntity
    {
        public int IngredientsTypeId { get; private set; }
        public string IngredientsTypeName { get; private set; } = string.Empty;

        public IngredientsType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome do Tipo de Ingrediente é obrigatório.", nameof(name));
            }
            // ID = 0 (default) e será atribuído pela DB
            IngredientsTypeName = name;
        }

        private IngredientsType(int id, string name)
        {
            ValidateName(name);

            IngredientsTypeId = id;
            IngredientsTypeName = name;
        }

        public void UpdateName(string newName)
        {
            ValidateName(newName);

            IngredientsTypeName = newName;
        }

        private void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome do Tipo de Ingrediente é obrigatório.", nameof(name));
            }
            if (name.Length > 50)
            {
                throw new ArgumentException("O nome do Tipo de Ingrediente não pode exceder 50 caracteres.", nameof(name));
            }
        }

        public static IngredientsType Reconstitute(int id, string name)
        {
            return new IngredientsType(id, name);
        }

        public int GetId() => IngredientsTypeId;

        public void SetId(int id)
        {
            if (IngredientsTypeId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            IngredientsTypeId = id;
        }

        public bool GetIsActive() => true;
    }
}
