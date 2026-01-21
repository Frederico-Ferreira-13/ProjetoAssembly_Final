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

        [BindProperty]
        public string Message { get; set; } = string.Empty;
        [BindProperty]
        public int Rating { get; set; }   
        public int RecipeId { get; set; }    
    }
}