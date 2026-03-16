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
            var comment = new Comments(
                commentsId: reader.GetInt32(reader.GetOrdinal("CommentsId")),
                recipesId: reader.GetInt32(reader.GetOrdinal("RecipesId")),
                userId: reader.GetInt32(reader.GetOrdinal("UserId")),
                commentText: reader.IsDBNull(reader.GetOrdinal("CommentText")) ? null : reader.GetString(reader.GetOrdinal("CommentText")),
                rating: reader.GetInt32(reader.GetOrdinal("Rating")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                lastUpdatedAt: reader.IsDBNull(reader.GetOrdinal("LastUpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastUpdatedAt")),
                isEdited: reader.GetBoolean(reader.GetOrdinal("IsEdited")),
                isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                originalComment: reader.IsDBNull(reader.GetOrdinal("OriginalComment")) ? null : reader.GetString(reader.GetOrdinal("OriginalComment")),
                parentCommentId: reader.IsDBNull(reader.GetOrdinal("ParentCommentId")) ? null : reader.GetInt32(reader.GetOrdinal("ParentCommentId"))
            );

            try
            {
                comment.UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName"));
                comment.Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name"));
            }
            catch (IndexOutOfRangeException)
            {
                
            }

            return comment;
        }

        protected override string BuildInsertSql(Comments entity)
        {
            return $@"INSERT INTO {_tableName}
                      (RecipesId, UserId, CommentText, Rating, CreatedAt,
                       LastUpdatedAt, IsEdited, IsDeleted, OriginalComment, ParentCommentId)
                      VALUES
                      (@RecipesId, @UserId, @CommentText, @Rating, @CreatedAt, 
                       @LastUpdatedAt, @IsEdited, @IsDeleted, @OriginalComment, @ParentCommentId);
                      SELECT CAST (SCOPE_IDENTITY() as int);";
        }

        protected override SqlParameter[] GetInsertParameters(Comments entity)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@RecipesId", entity.RecipesId),
                new SqlParameter("@UserId", entity.UserId),
                new SqlParameter("@CommentText",(object?)entity.CommentText ?? DBNull.Value),
                new SqlParameter("@Rating", entity.Rating),
                new SqlParameter("@CreatedAt", entity.CreatedAt),
                new SqlParameter("@LastUpdatedAt", (object?)entity.LastUpdatedAt ?? DBNull.Value),
                new SqlParameter("@IsEdited", entity.IsEdited),
                new SqlParameter("@IsDeleted", entity.IsDeleted),
                new SqlParameter("@OriginalComment", (object?)entity.OriginalComment ?? DBNull.Value),
                new SqlParameter("@ParentCommentId", (object?)entity.ParentCommentId ?? DBNull.Value)
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
                          OriginalComment = @OriginalComment                     
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
            string sql = $@"SELECT c.CommentsId, c.RecipesId, c.UserId, c.Rating, c.CommentText, 
                                   c.CreatedAt, c.LastUpdatedAt, c.IsEdited, c.IsDeleted, 
                                   c.OriginalComment, c.ParentCommentId,
                                   u.UserName, u.Name 
                            FROM {_tableName} c
                            INNER JOIN Users u ON c.UserId = u.UserId
                            WHERE c.RecipesId = @RecipesId AND c.IsDeleted = 0
                            ORDER BY c.CreatedAt ASC";

            var parameters = new SqlParameter[] { new SqlParameter("@RecipesId", recipeId) };
            var result = await ExecuteListAsync(sql, parameters);

            foreach (var c in result)
            {
                Console.WriteLine($"[DB LOAD] ID: {c.CommentsId}, Parent: {c.ParentCommentId}, Text: {c.CommentText}");
            }

            return result.ToList();
        }
    }    
}
