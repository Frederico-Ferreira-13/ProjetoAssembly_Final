using Contracts.Repository;
using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class registModel : PageModel
    {
        private readonly IUsersService _usersService;       

        public registModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [BindProperty]
        public string UserName { get; set; }
        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var dummyHash = new string('0', 60);
                var dummySalt = new string('0', 16);

                var newUserRequest = new Users(
                    UserName,
                    Email,
                    dummyHash,
                    dummySalt,
                    usersRoleId: 1,
                    isApproved: false,
                    accountId: 1
                );

                
                var result = await _usersService.RegisterUserAsync(newUserRequest, Password);

                if (result.IsSuccessful)
                {
                    return RedirectToPage("/Login");
                }

                ModelState.AddModelError(string.Empty, result.Error.Message);
            }
            catch (ArgumentException ex)
            {
               
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return Page();
        }
    }
}
