document.addEventListener("DOMContentLoaded", () => {
    const addIngredientBtn = document.getElementById("btn-add-ingredient");
    const ingredientList = document.getElementById("ingredient-list");

    if (addIngredientBtn && ingredientList) {
        addIngredientBtn.addEventListener("click", () => {
            const row = document.createElement("div");
            row.className = "ingrediente-linha";

            row.style.display = "flex";
            row.style.gap = "10px";
            row.style.marginBottom = "10px";

            div.innerHTML = `
                <input type="text"
                       name="ingredientes"
                       placeholder="Ex: 500g de Farinha"
                       required
                       style="flex: 1;"
                       class="edit-input">
                <button type="button" class="btn-remove-row" style="background: #dc3545; color: white; border: none; padding: 5px 10px; border-radius: 4px; cursor: pointer;">
                        <i class"fas fa-times"></i>
                </button>
            `;

            ingredientList.appendChild(div);

            row.querySelector('.btn-remove-row').onclick = () => div.remove();
        });
    }

    const iconTriggerBtn = document.querySelector("butn-active-icons");
    if (iconTriggerBtn) {
        iconTriggerBtn.addEventListener("click", () => {
            const icons = document.querySelectorAll("i[class*=icon-]");
            icons.forEach(icon => {
                const badge = document.createElement("span");
                badge.style.color = "var(--primary-green)"
                badge.innerHTML = "[Icon Active]";
                element.replaceWith(badge);
            });
        });
    }
});