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

        public Difficulty(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("O nome da dificuldade é obrigatório.", nameof(name));
            }

            DifficultyName = name;            
        }

        private Difficulty(int id, string name)
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
        }

        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("O novo nome da dificuldade é obrigatório.", nameof(newName));
            }
            DifficultyName = newName;
        }

        public static Difficulty Reconstitute(int id, string name)
        {
            return new Difficulty(id, name);
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

        public bool GetIsActive() => true;
    }
}
