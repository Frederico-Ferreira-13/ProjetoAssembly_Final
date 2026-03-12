using System;
using System.Collections.Generic;

namespace Core.Model
{
    public static class MockRecipes
    {
        public static HashSet<int> FavoriteMockIds { get; } = new HashSet<int>();

        public static IEnumerable<Recipes> GetFallbackMockRecipes()
        {
            var mockRecipes = new List<Recipes>();

            var arrozDoce = new Recipes(
                userId: 1,
                categoriesId: 1,
                difficultyId: 1,
                title: "Arroz Doce Cremoso",
                instructions: "1. Coza o arroz em leite com casca de limão e canela.\n" +
                              "2. Junte açúcar e gemas batidas no final.\n" +
                              "3. Polvilhe com canela em pó.",
                prepTimeMinutes: 10,
                cookTimeMinutes: 45,
                servings: "6 pessoas"
            );
            arrozDoce.SetId(9991);
            arrozDoce.AverageRating = 4.8;
            arrozDoce.FavoriteCount = 12;
            arrozDoce.SetImageUrl("arroz-doce.jpg");

            var arrozPato = new Recipes(
                userId: 1,
                categoriesId: 2,
                difficultyId: 2,
                title: "Arroz de Pato Tradicional",
                instructions: "1. Coza o pato com enchidos e especiarias.\n" +
                              "2. Use a gordura para refogar o arroz.\n" +
                              "3. Misture tudo e leve ao forno até dourar.",
                prepTimeMinutes: 30,
                cookTimeMinutes: 90,
                servings: "4 pessoas"
            );
            arrozPato.SetId(9992);
            arrozPato.AverageRating = 4.6;
            arrozPato.FavoriteCount = 85;
            arrozPato.SetImageUrl("arroz-de-pato.jpg");

            var sopa = new Recipes(
                userId: 1,
                categoriesId: 3,
                difficultyId: 1,
                title: "Sopa de Legumes Caseira e Reconfortante",
                instructions: "1. Descasque e corte os legumes em pedaços.\n" +
                              "2. Coza em água com sal e ervas.\n" +
                              "3. Triture com varinha mágica e finalize com azeite.",
                prepTimeMinutes: 15,
                cookTimeMinutes: 35,
                servings: "6 pessoas"
            );
            sopa.SetId(9993);
            sopa.AverageRating = 5.0;
            sopa.FavoriteCount = 200;
            sopa.SetImageUrl("sopa.jpg");

            var bacalhau = new Recipes(
                userId: 1,
                categoriesId: 4,
                difficultyId: 2,
                title: "Bacalhau à Brás",
                instructions: "1. Dessalgue e desfie o bacalhau.\n" +
                              "2. Refogue cebola e alho, junte batata palha e bacalhau.\n" +
                              "3. Envolva com ovos batidos e salsa.",
                prepTimeMinutes: 20,
                cookTimeMinutes: 25,
                servings: "4 pessoas"
            );
            bacalhau.SetId(9994);
            bacalhau.AverageRating = 4.7;
            bacalhau.FavoriteCount = 150;
            bacalhau.SetImageUrl("bacalhau.jpg");

            var bolo = new Recipes(
                userId: 1,
                categoriesId: 5,
                difficultyId: 3,
                title: "Bolo de Chocolate Vegan Fofinho",
                instructions: "1. Misture farinha, cacau, açúcar e fermento.\n" +
                              "2. Adicione leite vegetal, óleo e vinagre.\n" +
                              "3. Asse até ficar fofo e cubra com ganache vegan.",
                prepTimeMinutes: 15,
                cookTimeMinutes: 35,
                servings: "10 fatias"
            );
            bolo.SetId(9995);
            bolo.AverageRating = 4.6;
            bolo.FavoriteCount = 95;
            bolo.SetImageUrl("bolo-de-chocolate-vegan.jpg");

            mockRecipes.AddRange(new[] { arrozDoce, arrozPato, sopa, bacalhau, bolo });

            return mockRecipes;
        }
    }
}
