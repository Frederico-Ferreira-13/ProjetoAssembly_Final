using Contracts.Service;
using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoAssembly_Final.Pages
{
    public class recipesModel : PageModel
    {
        private readonly IRecipesService _recipesService;
        private readonly ITokenService _tokenService;

        public recipesModel(IRecipesService recipesService, ITokenService tokenService)
        {
            _recipesService = recipesService;
            _tokenService = tokenService;
        }

        public IEnumerable<Recipes> ListRecipes { get; set; } = new List<Recipes>();

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int P { get; set; } = 1; // Página atual (default = 1)

        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {
            int pageSize = 9;
            if (P < 1) P = 1;

            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            int? currentUserId = userIdResult.IsSuccessful ? userIdResult.Value : null;

            var result = await _recipesService.SearchRecipesAsync(Search, CategoryId, P, pageSize, currentUserId);

            if (result.IsSuccessful && result.Value.Items != null)
            {                
                ListRecipes = result.Value.Items;               
                TotalPages = (int)Math.Ceiling(result.Value.TotalCount / (double)pageSize);
            }
            else
            {
                ListRecipes = GetMockRecipes();
                TotalPages = 0;
            }
        }

        private IEnumerable<Recipes> GetMockRecipes()
        {
            return new List<Recipes>
            {
                Recipes.Reconstitute(
                    id: 9991,
                    userId: 1,
                    categoriesId: 1,
                    difficultyId: 1,
                    title: "Arroz Doce Cremoso",
                    instructions: "1. Lave 200g de arroz carolino e coza em 500ml de água com sal durante 10 minutos.\n" +
                                  "2. Aqueça 1 litro de leite com pau de canela, casca de limão e 200g de açúcar.\n" +
                                  "3. Quando ferver, junte o arroz semi-cozido e mexa em lume brando.\n" +
                                  "4. Cozinhe 30-40 minutos mexendo até cremoso (adicione leite se secar).\n" +
                                  "5. Retire do lume, junte 1 gema (opcional) e misture.\n" +
                                  "6. Coloque em taças, polvilhe com canela e sirva morno ou frio.",
                    prepTimeMinutes: 10,
                    cookTimeMinutes: 45,
                    servings: "6 pessoas",
                    imageUrl: "arroz-doce.jpg",
                    createdAt: DateTime.Now,
                    lastUpdatedAt: null,
                    isActive: true,
                    favoriteCount: 12,
                    averageRating: 4.8
                ),

                Recipes.Reconstitute(
                    id: 9992,
                    userId: 1,
                    categoriesId: 2,
                    difficultyId: 2,
                    title: "Arroz de Pato",
                    instructions: "1. Coza 1 pato inteiro em água temperada com sal, cebola, alho, louro e salsa durante 45-60 minutos.\n" +
                                  "2. Retire o pato, deixe arrefecer e desfie a carne (reserve o caldo).\n" +
                                  "3. Refogue cebola, alho e rodelas de chouriço em azeite.\n" +
                                  "4. Junte 400g de arroz e envolva bem.\n" +
                                  "5. Adicione 800ml do caldo reservado e tempere com sal, pimenta e colorau.\n" +
                                  "6. Coza o arroz 15-18 minutos em lume médio.\n" +
                                  "7. Nos últimos minutos, junte o pato desfiado e misture.\n" +
                                  "8. Finalize com salsa fresca e sirva quente.",
                    prepTimeMinutes: 30,
                    cookTimeMinutes: 90,
                    servings: "4 pessoas",
                    imageUrl: "arroz-de-pato.jpg",
                    createdAt: DateTime.Now,
                    lastUpdatedAt: null,
                    isActive: true,
                    favoriteCount: 7,
                    averageRating: 3.8
                ),
                Recipes.Reconstitute(
                    id: 9993,
                    userId: 1,
                    categoriesId: 3,
                    difficultyId: 1,
                    title: "Sopa de Legumes Caseira e Reconfortante",
                    instructions: "1. Descasque e corte em cubos 2 cenouras médias, 1 abóbora pequena (ou 300g de abóbora), 1 cebola grande e 2 dentes de alho.\n" +
                                  "2. Num tacho grande, aqueça 2 colheres de sopa de azeite e refogue a cebola e o alho até ficarem translúcidos (cerca de 5 minutos).\n" +
                                  "3. Junte os legumes cortados, tempere com sal e pimenta e deixe saltear 3 minutos.\n" +
                                  "4. Cubra com 1,5 L de água ou caldo de legumes e leve ao lume médio até ferver.\n" +
                                  "5. Reduza o lume, tape e coza durante 25-30 minutos até os legumes estarem bem macios.\n" +
                                  "6. Triture tudo com a varinha mágica até obter um creme homogéneo. Se ficar muito espesso, adicione um pouco mais de água.\n" +
                                  "7. Retifique os temperos, finalize com um fio de azeite cru e sirva quente com torradas ou croutons.",
                    prepTimeMinutes: 15,
                    cookTimeMinutes: 35,
                    servings: "6 pessoas",
                    imageUrl: "sopa.jpg",
                    createdAt: DateTime.Now,
                    lastUpdatedAt: null,
                    isActive: true,
                    favoriteCount: 20,
                    averageRating: 5.0
                ),
                Recipes.Reconstitute(
                    id: 9994,
                    userId: 1,
                    categoriesId: 4,
                    difficultyId: 2,
                    title: "Bacalhau à Brás",
                    instructions: "1. Dessalgue 500g de bacalhau durante 24 horas (mude a água várias vezes).\n" +
                                  "2. Coza o bacalhau em água abundante por 8-10 minutos (não deve desfazer-se completamente). Escorra, deixe arrefecer ligeiramente e desfie em lascas grandes (retire peles e espinhas).\n" +
                                  "3. Numa frigideira larga ou tacho, aqueça 4-5 colheres de sopa de azeite e refogue 2 cebolas grandes cortadas em juliana fina até ficarem bem douradinhas e doces (cerca de 10-12 minutos em lume médio).\n" +
                                  "4. Adicione 2 dentes de alho picados e deixe perfumar 1 minuto.\n" +
                                  "5. Junte o bacalhau desfiado e envolva delicadamente no refogado por 2-3 minutos (sem mexer muito para não desfazer).\n" +
                                  "6. Adicione 300-400g de batata palha pré-frita (ou faça fresca em casa) e misture bem para incorporar os sabores.\n" +
                                  "7. Bata 4-5 ovos com uma pitada de sal e pimenta e verta sobre o preparado. Mexa em lume brando até os ovos ficarem cremosos (não devem secar — deve ficar húmido e brilhante).\n" +
                                  "8. Retifique os temperos, polvilhe com salsa fresca picada e sirva imediatamente com azeitonas pretas e um bom vinho branco.",
                    prepTimeMinutes: 20,
                    cookTimeMinutes: 25,
                    servings: "4 pessoas",
                    imageUrl: "bacalhau.jpg",
                    createdAt: DateTime.Now,
                    lastUpdatedAt: null,
                    isActive: true,
                    favoriteCount: 15,
                    averageRating: 4.7
                ),
                Recipes.Reconstitute(
                    id: 9995,
                    userId: 1,
                    categoriesId: 5,
                    difficultyId: 3,
                    title: "Bolo de Chocolate Vegan Fofinho",
                    instructions: "1. Misture farinha, açúcar mascavado, cacau em pó, fermento e bicarbonato. " +
                                  "2. Adicione leite vegetal, óleo de coco, vinagre de maçã e extrato de baunilha. " +
                                  "3. Mexa até ficar homogêneo e leve ao forno a 180°C por 30-35 minutos. " +
                                  "4. Cubra com ganache de chocolate negro derretido com creme de aveia.",
                    prepTimeMinutes: 15,
                    cookTimeMinutes: 35,
                    servings: "10 fatias",
                    imageUrl: "bolo-de-chocolate-vegan.jpg",
                    createdAt: DateTime.Now,
                    lastUpdatedAt: null,
                    isActive: true,
                    favoriteCount: 15,
                    averageRating: 4.6
                )
            };
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int recipeId) 
        {
            var userIdResult = await _tokenService.GetUserIdFromContextAsync();
            if (!userIdResult.IsSuccessful)
            {
                return Unauthorized();
            }

            try
            {
                var result = await _recipesService.ToggleFavoriteAsync(recipeId, userIdResult.Value);
                if (result.IsSuccessful)
                {
                    return new JsonResult(result.Value);
                }
                else
                {                   
                    Console.WriteLine($"Toggle failed for recipeId {recipeId}: {result.Error}");
                    return BadRequest(result.Error); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in toggle: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
