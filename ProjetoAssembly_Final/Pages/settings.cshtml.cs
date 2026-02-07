using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contracts.Service;
using Core.Model;
using System.Security.Claims;

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
        public string UserName { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string InputTheme { get; set; } = "Light";

        [BindProperty]
        public string InputLanguage { get; set; } = "pt-PT";

        [BindProperty]
        public bool InputNotifications { get; set; }

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
                UserName = userResult.Value.UserName;
                Email = userResult.Value.Email;
            }

            var settingsResult = await _usersService.GetSettingsByUserIdAsync(userId);
            if (settingsResult.IsSuccessful)
            {
                InputTheme = settingsResult.Value.Theme;
                InputLanguage = settingsResult.Value.Language;
                InputNotifications = settingsResult.Value.ReceiveNotifications;
            }            

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Se entrar aqui, é porque algum campo está a falhar a validação silenciosamente
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

            if (result.IsSuccessful)            {              

                var userResult = await _usersService.GetUserByIdAsync(userId);

                if (userResult.IsSuccessful)
                {
                    var userToUpdate = userResult.Value;

                    userToUpdate.UpdateUserName(UserName);
                    userToUpdate.UpdateEmail(Email);

                    var userUpdateResult = await _usersService.UpdateUserProfileAsync(userToUpdate);
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

                TempData["SuccessMessage"] = "Perfil atualizado com sucesso";
                return RedirectToPage("/Perfil");
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Ocurreu um erro inesperado.");

            if(result.ValidationErrors != null)
            {
                foreach (var error in result.ValidationErrors)
                {
                    foreach (var message in error.Value)
                    {
                        ModelState.AddModelError(error.Key, message);
                    }
                }
            }

            return Page();
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
