using Core.Model;
using Contracts.Repository;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class CommentsRepository : Repository<Comments>, ICommentsRepository
    {
        protected override string PrimaryKeyName => "CommentsId";
        public CommentsRepository() : base("Comments") { }

        protected override Comments MapFromReader(SqlDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("CommentsId"));
            int recipesId = reader.GetInt32(reader.GetOrdinal("RecipesId"));
            int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
            int rating = reader.GetInt32(reader.GetOrdinal("Rating"));
            string? commentText = reader.IsDBNull(reader.GetOrdinal("CommentText")) ? null : reader.GetString(reader.GetOrdinal("CommentText"));

            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            DateTime? lastUpdatedAt = reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt"));
            bool isEdited = reader.GetBoolean(reader.GetOrdinal("IsEdited"));
            bool isDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"));
            string? originalComments = reader.IsDBNull(reader.GetOrdinal("OriginalComment")) ? null : reader.GetString(reader.GetOrdinal("OriginalComment"));
                       
            return Comments.Reconstitute(
                id,                
                recipesId,
                userId,
                commentText,
                rating,                
                createdAt,
                lastUpdatedAt,
                isEdited,
                isDeleted,
                originalComments
            );
        }

        protected override string BuildInsertSql(Comments entity)
        {
            return $@"INSERT INTO {_tableName} (RecipesId, UserId, CommentText, OriginalComment, IsDeleted, IsEdited)
                      VALUES (@RecipesId, @UserId, @CommentText, @Rating, GETDATE(), 0, 0, @OriginalComment)";
        }

        protected override SqlParameter[] GetInsertParameters(Comments entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@CommentText",(object?)entity.CommentText ?? DBNull.Value),
                new SqlParameter("@Rating", entity.Rating),
                new SqlParameter("@OriginalComment", (object?)entity.OriginalComment ?? DBNull.Value)
            };
        }

        protected override string BuildUpdateSql(Comments entity)
        {
            return $@"UPDATE {_tableName} 
                      SET CommentText = @CommentText,
                          Rating = @Rating,
                          LastUpdatedAt = GETDATE(),
                          IsEdited = @IsEdited,
                          IsDeleted = @IsDeleted,
                          OriginalComment = @OriginalComment,                     
                      WHERE CommentsId = @CommentsId";
        }

        protected override SqlParameter[] GetUpdateParameters(Comments entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@CommentText", (object?)entity.CommentText ?? DBNull.Value),
                new SqlParameter("@Rating", entity.Rating),
                new SqlParameter("@IsEdited", entity.IsEdited),
                new SqlParameter("@IsDeleted", entity.IsDeleted),
                new SqlParameter("@OriginalComment", (object?)entity.OriginalComment ?? DBNull.Value),
                new SqlParameter("@CommentsId", entity.GetId())
            };
        }

        public async Task<List<Comments>> GetCommentsByRecipeIdAsync(int recipeId)
        {
            string sql = $@"SELECT CommentsId, RecipesId, UserId, Rating, CommentText, CreatedAt, LastUpdatedAt, IsEdited, IsDeleted, OriginalComment 
                            FROM {_tableName} 
                            WHERE RecipesId = @RecipesId AND IsDeleted = 0 
                            ORDER BY CreatedAt DESC";

            return (await ExecuteListAsync(sql, new SqlParameter("@RecipesId", recipeId))).ToList();
        }
    }    
}
