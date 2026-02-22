using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contracts.Service;
using Core.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace ProjetoAssembly_Final.Pages
{
    public class settingsModel : PageModel
    {
       
        private readonly IUsersService _usersService;

        public settingsModel(IUsersService usersService)
        {            
            _usersService = usersService;
        }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string UserName { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string InputTheme { get; set; } = "Light";

        [BindProperty]
        public string InputLanguage { get; set; } = "pt-PT";

        [BindProperty]
        public bool InputNotifications { get; set; }

        [BindProperty]
        public string? CurrentPassword { get; set; }

        [BindProperty]
        public string? NewPassword { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if(userId == 0)
            {
                return RedirectToPage("/Login");
            }

            var userResult = await _usersService.GetUserByIdAsync(userId);
            if (userResult.IsSuccessful)
            {
                Name = userResult.Value.Name;
                UserName = userResult.Value.UserName;
                Email = userResult.Value.Email;
            }

            var settingsResult = await _usersService.GetSettingsByUserIdAsync(userId);
            if (settingsResult.IsSuccessful)
            {
                InputTheme = settingsResult.Value.Theme;
                InputLanguage = settingsResult.Value.Language;
                InputNotifications = settingsResult.Value.NotificationsEnabled;
            }            

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {                
                return Page();
            }

            var userId = GetUserId();
            if(userId == 0)
            {
                return RedirectToPage("/Login");
            }

            var settingsUpdate = new UserSettings(
                userId, 
                InputTheme, 
                InputLanguage, 
                InputNotifications
            );

            var result = await _usersService.UpdateUserSettingsAsync(settingsUpdate);
            if (result.IsSuccessful)
            {              

                var userResult = await _usersService.GetUserByIdAsync(userId);
                if (userResult.IsSuccessful)
                {
                    var userToUpdate = userResult.Value;
                    userToUpdate.UpdateName(Name);
                    userToUpdate.UpdateUserName(UserName);
                    userToUpdate.UpdateEmail(Email);

                    var userUpdateResult = await _usersService.UpdateUserProfileAsync(userToUpdate);
                    if (!userUpdateResult.IsSuccessful)
                    {
                        ModelState.AddModelError(string.Empty, userUpdateResult.Message ?? "Erro ao atualizar perfil.");
                        return Page();
                    }
                }

                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    if (string.IsNullOrWhiteSpace(CurrentPassword))
                    {
                        ModelState.AddModelError("CurrentPassword", "É necessário informar a palavra-passe atual para alterar.");
                        return Page();
                    }

                    if (NewPassword != ConfirmPassword)
                    {
                        ModelState.AddModelError("NewPassword", "As palavras-passe não coincidem.");
                        return Page();
                    }

                    if (NewPassword.Length < 8)
                    {
                        ModelState.AddModelError("NewPassword", "A palavra-passe deve ter pelo menos 8 caracteres.");
                        return Page();
                    }

                    var passwordResult = await _usersService.ChangeUserPasswordAsync(userId, CurrentPassword, NewPassword);
                    if (!passwordResult.IsSuccessful)
                    {
                        ModelState.AddModelError("NewPassword", passwordResult.Message ?? "Erro ao atualizar a palavra-passe.");
                        return Page();
                    }
                }

                // Lógica para o idioma mudar
                Response.Cookies.Append(
                    Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                    Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                        new Microsoft.AspNetCore.Localization.RequestCulture(InputLanguage)),
                    new CookieOptions { 
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        Path = "/",
                        HttpOnly = false
                    }
                );
                
            }
            TempData["SuccessMessage"] = "Perfil atualizado com sucesso";
            return RedirectToPage("/Perfil");

            
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
