using Contracts.Repository;
using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class registModel : PageModel
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IPasswordHasher _passwordHasher;

        public registModel(IUsersRepository usersRepository, IPasswordHasher passwordHasher)
        {
            _usersRepository = usersRepository;
            _passwordHasher = passwordHasher;
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

            string salt = _passwordHasher.GenerateSalt();
            var hashResult = _passwordHasher.HashPassword(Password, salt);

            if (!hashResult.IsSuccessful)
            {
                ModelState.AddModelError(string.Empty, "Erro ao processar password.");
                return Page();
            }

            int roleIdParaAtribuir = (Email.ToLower() == "fredericocrf87@hotmail.com") ? 2 : 1;

            bool aprovadoAutomatico = (roleIdParaAtribuir == 2);

            try
            {
                var newUser = new Users(
                    UserName,
                    Email,
                    hashResult.Value.Hash,
                    salt,
                    usersRoleId: roleIdParaAtribuir,
                    isApproved: aprovadoAutomatico,
                    accountId: 1
                );

                await _usersRepository.CreateAddAsync(newUser);
                return RedirectToPage("/login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error ao registar: " + ex.Message);
                return Page();
            }
        }
    }
}
