using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Service
{
    public interface ICommentsService
    {
        Task<Result<Comments>> GetCommentsByIdAsync(int id);
        Task<Result<List<Comments>>> GetAllCommentsAsync();
        Task<Result<Comments>> CreateCommentsAsync(Comments newCommentDto);
        Task<Result> UpdateCommentsAsync(int id, Comments updateComments);
        Task<Result> DeleteCommentsAsync(int id);
        Task<Result<List<Comments>>> GetCommentsByRecipeIdAsync(int recipeId);
    }
}
