// --- 2. ADICIONAR DINAMICAMENTE INGREDIENTES (Página Criar Receita) ---
const addBtn = document.getElementById('btn-add-ingredient');
const list = document.getElementById('ingredient-list');

if (addBtn && list) {
    addBtn.addEventListener('click', () => {
        const row = document.createElement('div');

        // Verifica em que página estamos para aplicar a classe de input correta
        const isEditPage = document.querySelector('.edit-recipe-wrapper') !== null;
        const inputClass = isEditPage ? "form-input-custom" : "form-control-recipe";

        row.className = 'ingredient-row';
        row.innerHTML = `
                <input type="number" name="QuantityValue[]" step="0.01" placeholder="Qty" class="form-input-custom qty" />
                <input type="text" name="Unit[]" placeholder="Unit" class="form-input-custom unit" />
                <input type="text" name="IngredientName[]" placeholder="Ingredient Name" class="form-input-custom" required />
                <input type="text" name="ingredientDetail[]" placeholder="Note/Type" class="form-input-custom detail" />
                <button type="button" class="btn-remove">
                    <i class="fa-solid fa-trash"></i>
                </button>
            `;

        list.appendChild(linha);
        row.querySelector('.btn-remove').onclick = () => {
            linha.remove();
        };
    });
}