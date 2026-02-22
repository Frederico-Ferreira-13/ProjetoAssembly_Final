using Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Comments : IEntity
    {
        public int CommentsId { get; private set; }
        public int RecipesId { get; protected set; }
        public int UserId { get; protected set; }
        public string? CommentText { get; protected set; }
        public int Rating {  get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime? LastUpdatedAt { get; protected set; }
        public bool IsEdited { get; protected set; } = false;
        public bool IsDeleted { get; private set; } = false;
        public string? OriginalComment { get; private set; }

        private const int EditGracePeriodInMinutes = 5;

        [SetsRequiredMembers]
        private Comments() { }

        public Comments(int recipesId, int userId, string commentText, int rating)
        {
            if (recipesId <= 0) 
            {
                throw new ArgumentException("ID da receita inválido.", nameof(recipesId));
            }

            if (userId <= 0) 
            {
                throw new ArgumentException("ID do utilizador inválido.", nameof(userId));
            }
            if (string.IsNullOrWhiteSpace(commentText)) 
            {
                throw new ArgumentException("Comentário não pode ser vazio.", nameof(commentText));
            }
            if (commentText.Length > 500) 
            {
                throw new ArgumentException("Comentário não pode exceder 500 caracteres.", nameof(commentText));
            }
            if (rating < 1 || rating > 5) 
            {
                throw new ArgumentException("Rating deve ser entre 1 e 5.", nameof(rating));
            }

            RecipesId = recipesId;
            UserId = userId;
            CommentText = commentText;
            OriginalComment = commentText;
            Rating = rating;
            CreatedAt = DateTime.UtcNow;
            IsEdited = false;
            IsDeleted = false;
        }       

        public static Comments Reconstitute(int id, int recipesId, int userId, string? commentText, int rating, 
            DateTime createdAt, DateTime? lastUpdatedAt, bool isEdited, bool isDeleted, string? originalComment)
        {
            var comment = new Comments
            {
                CommentsId = id,
                RecipesId = recipesId,
                UserId = userId,
                CommentText = commentText,
                Rating = rating,
                CreatedAt = createdAt,
                LastUpdatedAt = lastUpdatedAt,
                IsEdited = isEdited,
                IsDeleted = isDeleted,
                OriginalComment = originalComment
            };

            return comment;
        }

        public void UpdateComment([NotNull] string newCommentText)
        {
            if (IsDeleted)
            {
                throw new InvalidOperationException("Não é possível editar um comentário eliminado.");
            }                

            if (string.IsNullOrWhiteSpace(newCommentText)) 
            { 
                throw new ArgumentException("Comentário não pode ser vazio.", nameof(newCommentText));
            }
                

            if (newCommentText.Length > 500)
            {
                throw new ArgumentException("Comentário não pode exceder 500 caracteres.", nameof(newCommentText));
            }                

            if ((DateTime.UtcNow - CreatedAt).TotalMinutes > EditGracePeriodInMinutes)
            {
                throw new InvalidOperationException($"Comentários só podem ser editados até {EditGracePeriodInMinutes} minutos após criação.");
            }                

            if (CommentText != newCommentText)
            {
                OriginalComment ??= CommentText;
                CommentText = newCommentText;
                IsEdited = true;
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

        public void Delete()
        {
            if (!IsDeleted)
            {
                IsDeleted = true;
                LastUpdatedAt = DateTime.UtcNow;                
                CommentText = "[Comentário eliminado pelo utilizador]";
            }
        }

        public int GetId() => CommentsId;

        public void SetId(int id)
        {
            if (CommentsId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            CommentsId = id;
        }

        public bool GetIsActive() => !IsDeleted;
    }
}
