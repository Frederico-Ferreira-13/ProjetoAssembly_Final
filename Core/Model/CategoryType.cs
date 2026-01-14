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
        public bool IsActive { get; private set; }

        public CategoryType(string name)
        {
            ValidateName(name);

            TypeName = name;
            this.IsActive = true;
        }

        private CategoryType(int id, string name, bool isActive)
        {
            CategoryTypeId = id;
            TypeName = name;
            this.IsActive = isActive;
        }

        public void UpdateName(string newName)
        {
            ValidateName(newName);

            if (TypeName != newName)
            {
                TypeName = newName;
            }
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
            }
        }

        public static CategoryType Reconstitute(int id, string name, bool isActive)
        {
            return new CategoryType(id, name, isActive);
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

        public bool GetIsActive() => IsActive;
    }
}
