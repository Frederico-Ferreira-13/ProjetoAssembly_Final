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
            return new Comments(
                commentsId: reader.GetInt32(reader.GetOrdinal("CommentsId")),
                recipesId: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                userId: reader.GetInt32(reader.GetOrdinal("UserId")),
                commentText: reader.IsDBNull(reader.GetOrdinal("CommentText")) ? null : reader.GetString(reader.GetOrdinal("CommentText")),
                rating: reader.GetInt32(reader.GetOrdinal("Rating")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                lastUpdatedAt: reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt")),
                isEdited: reader.GetBoolean(reader.GetOrdinal("IsEdited")),
                isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                originalComment: reader.IsDBNull(reader.GetOrdinal("OriginalComment")) ? null : reader.GetString(reader.GetOrdinal("OriginalComment"))
            );
        }

        protected override string BuildInsertSql(Comments entity)
        {
            return $@"UPDATE {_tableName}
                      SET CommentText = @CommentText,
                          Rating = @Rating,
                          LastUpdatedAt = GETDATE(),
                          IsEdited = @IsEdited,
                          IsDeleted = @IsDeleted,
                          OriginalComment = @OriginalComment
                      WHERE CommentsId = @CommentsId";
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

            var parameters = new SqlParameter[] { new SqlParameter("@RecipesId", recipeId) };

            var result = await ExecuteListAsync(sql, parameters);
            return result.ToList();
        }
    }    
}
