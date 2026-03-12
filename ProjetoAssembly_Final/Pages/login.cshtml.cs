using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core.Common;
using Core.Model;
using Contracts.Service;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjetoAssembly_Final.Pages
{
    public class loginModel : PageModel
    {
        private readonly IUsersService _usersService;
        private readonly ITokenService _tokenService;

        public loginModel(IUsersService usersService, ITokenService tokenService)
        {
            _usersService = usersService;
            _tokenService = tokenService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Identifier { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet()
        {            
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var authResult = await _usersService.AuthenticateUserAsync(Input.Identifier, Input.Password);

            if (!authResult.IsSuccessful || authResult.Value == null)
            {
                ModelState.AddModelError(string.Empty, authResult.Message ?? "Utilizador ou palavra-passe incorretos.");
                return Page();
            }

            var user = authResult.Value;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UsersRoleId == 1 ? "Admin" : "User"),
                new Claim("IsApproved", user.IsApproved.ToString())               
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // "Lembrar-me"
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                RedirectUri = returnUrl ?? "/Index"
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return LocalRedirect(authProperties.RedirectUri ?? "/Index");
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }        
    }
}
