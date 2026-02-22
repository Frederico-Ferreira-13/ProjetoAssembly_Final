using Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Favorites : IEntity
    {
        public int FavoritesId { get; set; }
        public int UserId { get; set; }
        public int RecipesId { get; set; }
        public DateTime CreatedAt { get; set; }        

        public Users? User { get; set; }
        public Recipes? Recipe { get; set; }

        public Favorites() { }

        public Favorites(int userId, int recipesId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("O UserId deve ser um ID válido (maior que 0).", nameof(userId));
            }

            if (recipesId <= 0)
            {
                throw new ArgumentException("O RecipesId deve ser um ID válido (maior que 0).", nameof(recipesId));
            }

            CreatedAt = DateTime.UtcNow;
           
            UserId = userId;
            RecipesId = recipesId;
            CreatedAt = DateTime.UtcNow;            
        }

        [SetsRequiredMembers]
        public Favorites(int id, int userId, int recipesId, DateTime createdAt)
        {
            if (id < 0)
            {
                throw new ArgumentException("O ID não pode ser negativo.", nameof(id));
            }

            FavoritesId = id;
            UserId = userId;
            RecipesId = recipesId;
            CreatedAt = createdAt;            
        }

        public static Favorites Reconstitute(int id, int userId, int recipesId, DateTime createdAt)
        {
            return new Favorites(id, userId, recipesId, createdAt);
        }

        public bool IsValid()
        {
            return UserId > 0 && RecipesId > 0;
        }

        public int GetId() => FavoritesId;

        public void SetId(int id)
        {
            if (FavoritesId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            FavoritesId = id;
        }      

        public bool GetIsActive() => true;
    }
}
