using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Xml.Linq;

namespace ProjetoAssembly_Final.Pages
{
    public class commentsModel : PageModel
    {

        private readonly ICommentsService _commentsService;

        public commentsModel(ICommentsService commentsService)
        {
            _commentsService = commentsService;
        }

        public List<Comments> ListComments { get; set; } = new();

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public int Rating { get; set; }

        [BindProperty(SupportsGet = true)] //Permite receber o ID via URL
        public int RecipeId { get; set; }

        public async Task OnGetAsync()
        {
            if (RecipeId > 0)
            {
                var result = await _commentsService.GetCommentsByRecipeIdAsync(RecipeId);
                if (result.IsSuccessful && result.Value != null)
                {
                    ListComments = result.Value;
                }
            }
            else
            {
                var result = await _commentsService.GetAllCommentsAsync();
                if (result.IsSuccessful && result.Value != null)
                {
                    ListComments = result.Value;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out int userId))
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }            

            try
            {
                var newComments = new Comments(userId, RecipeId, Message);

                var result = await _commentsService.CreateCommentsAsync(newComments);

                if (result.IsSuccessful)
                {
                    return RedirectToPage(new { RecipeId = RecipeId });
                }
                ModelState.AddModelError(string.Empty, "Erro ao publicar comentário.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return Page();
        }         
    }
}
