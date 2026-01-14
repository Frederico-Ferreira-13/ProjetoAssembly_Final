using Contracts.Repository;
using Core.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Repo.Repository
{
    public class CommentsRepository : Repository<Comments>, ICommentsRepository
    {
        public CommentsRepository() : base("Comments")
        {
        }

        protected override Comments MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("CommentsId"));

            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            DateTime? lastUpdatedAt = reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt"));

            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

            int recipesId = reader.GetInt32(reader.GetOrdinal("RecipesId"));
            int userId = reader.GetInt32(reader.GetOrdinal("UserId"));

            string? commentText = reader.IsDBNull(reader.GetOrdinal("CommentText")) ? null : reader.GetString(reader.GetOrdinal("CommentText"));
            string? originalComments = reader.IsDBNull(reader.GetOrdinal("OriginalComment")) ? null : reader.GetString(reader.GetOrdinal("OriginalComment"));

            bool isDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"));
            bool isEdited = reader.GetBoolean(reader.GetOrdinal("IsEdited"));

            return Comments.Reconstitute(
                id,
                isActive,
                recipesId,
                userId,
                commentText,
                createdAt,
                lastUpdatedAt,
                isEdited,
                isDeleted,
                originalComments
            );
        }

        protected override string BuildInsertSql(Comments entity)
        {
            return $"INSERT INTO {_tableName} (RecipesId, UserId, CommentText, OriginalComment, IsDeleted, IsEdited) " +
                $"VALUES (@RecipesId, @UserId, @CommentText, @OriginalComment, @IsDeleted, @IsEdited)";
        }

        protected override SqlParameter[] GetInsertParameters(Comments entity)
        {
            // O ParentCategoryId pode ser NULL
            object originalCommentValue = entity.OriginalComment != null ? (object)entity.OriginalComment : DBNull.Value;
            object commentTextValue = entity.CommentText != null ? (object)entity.CommentText : DBNull.Value;

            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@CommentText", SqlDbType.NVarChar, 4000) { Value = commentTextValue },
                new SqlParameter("@OriginalComment", SqlDbType.NVarChar, 4000) {Value = originalCommentValue },
                new SqlParameter("@IsDeleted", entity.IsDeleted),
                new SqlParameter("@IsEdited", entity.IsEdited)
            };
        }

        protected override string BuildUpdateSql(Comments entity)
        {
            return $"UPDATE {_tableName} SET CommentText = @CommentText, OriginalComment = @OriginalComment, " +
                   $"IsEdited = @IsEdited, IsDeleted = @IsDeleted, LastUpdatedAt = GETDATE() " +
                   $"WHERE CommentsId = @Id";
        }

        protected override SqlParameter[] GetUpdateParameters(Comments entity)
        {
            object originalCommentValue = entity.OriginalComment != null ? (object)entity.OriginalComment : DBNull.Value;
            object commentTextValue = entity.CommentText != null ? (object)entity.CommentText : DBNull.Value;

            return new SqlParameter[]
            {
                new SqlParameter("@CommentText", SqlDbType.NVarChar, 4000) { Value = commentTextValue },
                new SqlParameter("@OriginalComment", SqlDbType.NVarChar, 4000) { Value = originalCommentValue },
                new SqlParameter("@IsEdited", entity.IsEdited),
                new SqlParameter("@IsDeleted", entity.IsDeleted),
                new SqlParameter("@Id", entity.GetId())
            };
        }

        public async Task<List<Comments>> GetCommentsByRecipeIdAsync(int recipeId)
        {
            List<Comments> comments = new List<Comments>();

            string sql = $"SELECT * FROM {_tableName} WHERE RecipesId = @RecipesId AND IsActive = 1 ORDER BY CreatedAt DESC";

            SqlParameter paramRecipeId = new SqlParameter("@RecipesId", recipeId);

            try
            {
                // Reutilizando o método auxiliar do Repositório Genérico.
                return (await ExecuteListAsync(sql, paramRecipeId)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório GetCommentsByRecipeIdAsync: {ex.Message}");
                throw;
            }
        }
    }
}
