using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Service.Services;

namespace ProjetoAssembly_Final.Pages
{
    public class PerfilEditModel : PageModel
    {
        private readonly IUsersService _usersService;
        private readonly ICloudService _cloudService;

        public PerfilEditModel(IUsersService usersService, ICloudService cloudService)
        {
            _usersService = usersService;
            _cloudService = cloudService;
        }

        public Users CurrentUser { get; set; } = null!;

        [BindProperty]
        public IFormFile? PhotoUpload { get; set; }

        [BindProperty]
        public string InputUserName { get; set; } = string.Empty;

        [BindProperty]
        public string InputEmail { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userResult = await _usersService.GetCurrentUserAsync();

            if (!userResult.IsSuccessful || userResult.Value == null)
            {
                return RedirectToPage("/Login");
            }

            CurrentUser = userResult.Value!;

            InputUserName = CurrentUser.UserName;
            InputEmail = CurrentUser.Email;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Debug: Verificar se o método é sequer atingido
            Console.WriteLine("--- Iniciando OnPostAsync ---");

            var userResult = await _usersService.GetCurrentUserAsync();
            if (!userResult.IsSuccessful)
            {
                return RedirectToPage("/Login");
            }

            var userToUpdate = userResult.Value;

            // 2. Debug: Verificar a Imagem
            if (PhotoUpload != null && PhotoUpload.Length > 0)
            {
                Console.WriteLine($"--- Tentando upload: {PhotoUpload.FileName}, Tamanho: {PhotoUpload.Length} ---");

                try
                {
                    var imageUrl = await _cloudService.UploadImageAsync(PhotoUpload);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        Console.WriteLine($"--- Upload Sucesso! URL: {imageUrl} ---");
                        userToUpdate!.UpdateProfilePicture(imageUrl);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "O CloudService devolveu uma URL vazia.");
                        Console.WriteLine("--- Erro: URL da imagem veio vazia ---");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Erro no CloudService: {ex.Message}");
                    Console.WriteLine($"--- EXCEÇÃO NO CLOUDSERVICE: {ex.Message} ---");
                }
            }
            else if (PhotoUpload == null)
            {
                Console.WriteLine("--- Debug: PhotoUpload está NULL ---");
            }

            try
            {
                userToUpdate!.UpdateUserName(InputUserName);
                userToUpdate.UpdateEmail(InputEmail);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Erro na validação do Modelo: {ex.Message}");
                CurrentUser = userToUpdate!;
                return Page();
            }

            // 3. Debug: Verificar a gravação final na BD
            var result = await _usersService.UpdateUserProfileAsync(userToUpdate);

            if (result.IsSuccessful)
            {
                Console.WriteLine("--- Perfil gravado na BD com sucesso! Redirecionando... ---");
                TempData["Success"] = "Perfil atualizado com sucesso!";
                return RedirectToPage("/perfil");
            }

            // 4. Debug: Se chegou aqui, a gravação na BD falhou
            Console.WriteLine("--- Erro: UpdateUserProfileAsync retornou falha ---");
            ModelState.AddModelError(string.Empty, "A base de dados não aceitou as alterações. Verifica se os campos são demasiado longos.");

            CurrentUser = userToUpdate!;
            return Page();
        }
    }
}
