document.addEventListener("DOMContentLoaded", () => {
    const btnAddIngrediente = document.getElementById("btn-add-ingrediente");
    const listaIngredientes = document.getElementById("lista-ingredientes");

    if (btnAddIngrediente) {
        btnAddIngrediente.addEventListener("click", () => {
            const div = document.createElement("div");
            div.className = "ingrediente-linha";
            div.style.display = "flex";
            div.style.gap = "10px";
            div.style.marginBottom = "10px";

            div.innerHTML = `
                <input type="text" name="ingredientes" placeholder="Ex: 500g de Farinha" required style="flex: 1;">
                <button type="button" class="btn-remover" style="background: #dc3545; color: white; border: none; padding: 5px 10px; border-radius: 4px; cursor: pointer;">X</button>
            `;
            listaIngredientes.appendChild(div);

            div.querySelector('.btn-remover').onclick = () => div.remove();
        });
    }

    const btnIcons = document.querySelector("button[type='button']");
    if (btnIcons) {
        btnIcons.addEventListener("click", () => {
            const elements = document.querySelectorAll("i[class*=icon-]");
            elements.forEach(element => {
                const svg = document.createElement("div");
                svg.innerHTML = `<span style="color: #28a745;">[Ícone Ativado]</span>`;
                element.replaceWith(svg.firstChild);
            });
        });
    }
});