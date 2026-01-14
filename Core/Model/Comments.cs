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
        public bool IsActive { get; private set; }

        public int RecipesId { get; protected set; }
        public int UserId { get; protected set; }

        public string? CommentText { get; protected set; }
        public bool IsDeleted { get; protected set; } = false;
        public bool IsEdited { get; protected set; } = false;
        public string? OriginalComment { get; protected set; }

        public DateTime CreatedAt { get; protected set; }
        public DateTime? LastUpdatedAt { get; protected set; }

        private const int EditGracePeriodInMinutes = 5;

        [SetsRequiredMembers]
        private Comments()
        {
            this.CommentsId = default;
            this.IsActive = true;
        }

        public Comments(int recipesId, int userId, [NotNull] string commentText)
        {
            ValidateFks(recipesId, userId);
            ValidateCommentText(commentText);

            this.CommentsId = default;
            this.IsActive = true;

            RecipesId = recipesId;
            UserId = userId;
            CommentText = commentText;

            this.CreatedAt = DateTime.UtcNow;

            IsDeleted = false;
            IsEdited = false;
            OriginalComment = commentText;
        }

        private Comments(int id, bool isActive, int recipesId, int userId, string? commentText,
            DateTime createdAt, DateTime? lastUpdatedAt, bool isEdited, bool isDeleted,
            string? originalComment)
        {
            this.CommentsId = id; ;
            this.IsActive = isActive;

            RecipesId = recipesId;
            UserId = userId;
            CommentText = commentText;

            this.CreatedAt = createdAt;
            this.LastUpdatedAt = lastUpdatedAt;

            IsEdited = isEdited;
            IsDeleted = isDeleted;
            OriginalComment = originalComment;
        }

        public static Comments Reconstitute(int id, bool isActive, int recipesId, int userId, string? commentText,
            DateTime createdAt, DateTime? lastUpdatedAt, bool isEdited, bool isDeleted, string? originalComment)
        {
            return new Comments(id, isActive, recipesId, userId, commentText, createdAt, lastUpdatedAt,
                        isEdited, isDeleted, originalComment);
        }

        public void UpdateComment([NotNull] string newCommentText)
        {
            ValidateCommentText(newCommentText); // Garante que o novo texto é válido

            if (DateTime.UtcNow.Subtract(this.CreatedAt).TotalMinutes > EditGracePeriodInMinutes)
            {
                throw new InvalidOperationException($"Os comentários só podem ser editados até {EditGracePeriodInMinutes} minutos após o envio.");
            }
            if (IsDeleted)
            {
                throw new InvalidOperationException("Comentários eliminados não podem ser editados.");
            }

            if (CommentText != newCommentText)
            {
                // Guarda o original apenas na primeira edição (se não houver OriginalComment)
                if (!IsEdited && OriginalComment == null)
                {
                    OriginalComment = CommentText;
                }

                CommentText = newCommentText;
                IsEdited = true;
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

        // [NotNull] Pode ser nulo, mas a função irá garantir que não é antes de regressar normalmente.
        private static void ValidateFks(int recipesId, int userId)
        {
            if (recipesId <= 0)
            {
                throw new ArgumentException("O ID da Receita associada é obrigatório.", nameof(recipesId));
            }
            if (userId <= 0)
            {
                throw new ArgumentException("O ID do Utilizador associado é obrigatório.", nameof(userId));
            }
        }

        private static void ValidateCommentText([NotNull] string? commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                throw new ArgumentException("O conteúdo do comentário não pode ser vazio.", nameof(commentText));
            }

            if (commentText.Length > 500)
            {
                throw new ArgumentException("O conteúdo do comentário não pode exceder 500 caracteres.", nameof(commentText));
            }
        }

        public void DeleteComment()
        {
            if (IsDeleted)
            {
                return;
            }

            if (!IsEdited)
            {
                OriginalComment = CommentText;
            }

            IsDeleted = true;
            CommentText = "[Comentário Eliminada]";
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            if (!IsDeleted)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(OriginalComment))
            {
                throw new InvalidOperationException("Não é possível restaurar. O conteúdo original não foi preservardo.");
            }

            CommentText = OriginalComment;
            IsDeleted = false;
            LastUpdatedAt = DateTime.UtcNow;
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

        public bool GetIsActive() => IsActive;
    }
}
