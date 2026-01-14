using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contracts.Service;
using Core.Model;
using System.Security.Claims;

namespace ProjetoAssembly_Final.Pages
{
    public class settingsModel : PageModel
    {

        private readonly IUserSettingsService _settingsService;
        private readonly IUsersService _usersService;

        public settingsModel(IUserSettingsService settingsService, IUsersService usersService)
        {
            _settingsService = settingsService;
            _usersService = usersService;
        }

        [BindProperty]
        public UserSettings CurrentSettings { get; set; } = null!;

        [BindProperty]
        public string UserName { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if(userId == null)
            {
                return RedirectToPage("/Login");
            }

            var settingsResult = await _settingsService.GetSettingsByUserIdAsync(userId);
            if (settingsResult.IsSuccessful)
            {
                CurrentSettings = settingsResult.Value;
            }

            var userResult = await _usersService.GetUserByIdAsync(userId);
            if (userResult.IsSuccessful)
            {
                UserName = userResult.Value.UserName;
                Email = userResult.Value.Email;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = GetUserId();
            if(userId == 0)
            {
                return RedirectToPage("/Login");
            }

            return RedirectToPage();
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
