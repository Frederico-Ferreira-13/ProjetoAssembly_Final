using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Repository
{
    public interface ICommentsRepository : IRepository<Comments>
    {
        Task<List<Comments>> GetCommentsByRecipeIdAsync(int recipeId);
    }
}
