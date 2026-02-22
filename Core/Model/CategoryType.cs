using Core.Common;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class CategoryType : IEntity
    {
        public int CategoryTypeId { get; private set; }
        public string TypeName { get; private set; } = string.Empty;        

        public CategoryType(string name)
        {
            ValidateName(name);

            TypeName = name;            
        }

        private CategoryType(int id, string name)
        {
            CategoryTypeId = id;
            TypeName = name;            
        }

        public void UpdateName(string newName)
        {
            ValidateName(newName);

            if (TypeName != newName)
            {
                TypeName = newName;
            }
        }

        public static CategoryType Reconstitute(int id, string name)
        {
            return new CategoryType(id, name);
        }

        private void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome do Tipo de categoria é obrigatório", nameof(name));
            }

            if (name.Length > 50)
            {
                throw new ArgumentException("O nome do Tipo de Categoria não pode exceder os 50 caracteres", nameof(name));
            }
        }

        public int GetId() => CategoryTypeId;

        public void SetId(int id)
        {
            if (CategoryTypeId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            CategoryTypeId = id;
        }

        public bool GetIsActive() => true;
    }
}
