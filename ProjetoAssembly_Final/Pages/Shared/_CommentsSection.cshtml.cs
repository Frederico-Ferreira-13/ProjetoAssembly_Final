using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages.Shared
{
    public class _CommentsSectionModel : PageModel
    {
        public List<Comments> ListComments { get; set; } = new();
        public int RecipeId { get; set; }

        public string? CommentMessage { get; set; }
        public int CommentRating { get; set; }
    }
}