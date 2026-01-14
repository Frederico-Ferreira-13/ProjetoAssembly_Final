using Core.Common;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Difficulty : IEntity
    {
        public int DifficultyId { get; private set; }
        public string DifficultyName { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }

        public Difficulty(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome da dificuldade é obrigatório.", nameof(name));
            }

            DifficultyName = name;
            this.IsActive = true;
        }

        private Difficulty(int id, string name, bool isActive)
        {
            if (id <= 0)
            {
                throw new ArgumentException("O ID da dificuldade é inválido para reconstituição.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome da dificuldade é obrigatório.", nameof(name));
            }

            DifficultyId = id;
            DifficultyName = name;
            this.IsActive = isActive;
        }

        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("O novo nome da dificuldade é obrigatório.", nameof(newName));
            }
            DifficultyName = newName;
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
            }
        }

        public static Difficulty Reconstitute(int id, string name, bool isActive) // 🎯 CORREÇÃO 4: Adicionar isActive ao Reconstitute
        {
            return new Difficulty(id, name, isActive);
        }

        public int GetId() => DifficultyId;

        public void SetId(int id)
        {
            if (DifficultyId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            DifficultyId = id;
        }

        public bool GetIsActive() => IsActive;
    }
}
