/* PROJETO: Receitas do Frederico
    DESCRIÇÃO: Lógica geral de navegação, slider, gestão de ingredientes e comunicação com a API.
*/

// --- 1. GESTÃO DE NAVEGAÇÃO (Active Link) ---
document.addEventListener('DOMContentLoaded', () => {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.container-nav ul li a');
    const themeSelect = document.querySelector('select[name="InputTheme"]');
    if (themeSelect) {
        themeSelect.addEventListener('change', (e) => {
            if (e.target.value === "Dark") {
                document.body.classList.add('dark-mode');
            } else {
                document.body.classList.remove('dark-mode');
            }
        });
    }
    navLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });

    // --- 2. ADICIONAR DINAMICAMENTE INGREDIENTES (Página Criar Receita) ---
    const btnAdd = document.getElementById('btn-add-ingrediente');
    const lista = document.getElementById('lista-ingredientes');

    if (btnAdd && lista) {
        btnAdd.addEventListener('click', () => {
            const linha = document.createElement('div');
            linha.className = 'ingrediente-linha';
            linha.innerHTML = `
                <input type="number" name="QuantityValue[]" step="0.01" placeholder="Qtd" class="form-input-custom qtd" required />
                <input type="text" name="Unit[]" placeholder="Unid." class="form-input-custom unit" required />
                <input type="text" name="IngredientName[]" placeholder="Nome do Ingrediente" class="form-input-custom" required />
                <button type="button" class="btn-remove" onclick="this.parentElement.remove()">
                    <i class="fa-solid fa-trash"></i>
                </button>
            `;

            lista.appendChild(linha);
            linha.querySelector('.btn-remove').onclick = () => {
                linha.remove();
            };
        });
    }

    // --- 3. PREVIEW DE IMAGEM (Receita ou Perfil) ---
    const recipeInput = document.getElementById('RecipeImage');
    const recipeImg = document.getElementById('image-preview');
    if (recipeInput && recipeImg) {
        recipeInput.addEventListener('change', function () {
            const file = this.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = (e) => recipeImg.src = e.target.result;
                reader.readAsDataURL(file);
            }
        });
    }

    // --- 4. SLIDER AUTOMÁTICO ---
    const slides = document.getElementsByClassName("mySlides");
    if (slides.length > 0) {
        showDivs(slideIndex);
        setInterval(() => { plusDivs(1); }, 5000);
    }
});

// --- FUNÇÕES GLOBAIS (Fora do DOMContentLoaded) ---

// Menu Dropdown do Perfil
function toggleMenu() {
    const menu = document.getElementById("dropdown-perfil");
    if (menu) {
        menu.classList.toggle("show");
    }
}

// Fechar menu ao clicar fora
window.onclick = function (event) {
    if (!event.target.closest('.perfil-menu')) {
        const dropdown = document.getElementById("dropdown-perfil");
        if (dropdown && dropdown.classList.contains('show')) {
            dropdown.classList.remove('show');
        }
    }
}

// Slider
var slideIndex = 1;
function plusDivs(n) { showDivs(slideIndex += n); }
function showDivs(n) {
    const x = document.getElementsByClassName("mySlides");
    if (x.length === 0) return;
    if (n > x.length) { slideIndex = 1 }
    if (n < 1) { slideIndex = x.length }
    for (let i = 0; i < x.length; i++) {
        x[i].style.display = "none";
    }
    x[slideIndex - 1].style.display = "block";
}

async function handleToggleFavorite(event, btn, recipeId) {    

    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        console.error("Token não encontrado!");
        return;
    }

    const formData = new FormData();
    formData.append("__RequestVerificationToken", token);

    try {
        const response = await fetch(`?handler=ToggleFavorite&recipeId=${recipeId}`, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const data = await response.json();
            const icon = btn.querySelector('i');
            const card = btn.closest('.receitas-card');

            if (data.isFavorite) {
                btn.classList.add('active');
                icon.classList.replace('fa-regular', 'fa-solid');
            } else {
                btn.classList.remove('active');
                icon.classList.replace('fa-solid', 'fa-regular');

                if (window.location.pathname.includes("MyFavoritsRecipes")) {
                    card.style.transition = "all 0.4s ease";
                    card.style.opacity = "0";
                    card.style.transform = "scale(0.8)";

                    setTimeout(() => {
                        card.remove();
                        const grid = document.getElementById('container-receitas');
                        if (grid && grid.querySelectorAll('.receitas-card').length === 0) {
                            location.reload();
                        }
                    }, 400);
                }
            }

            const countLabel = card.querySelector('.card-popularity');
            if (countLabel && data.newCount !== undefined) {
                countLabel.innerHTML = `<i class="fa-solid fa-heart"></i> ${data.newCount} favoritos`;
            }
        } else if (response.status === 401) {
            alert("Precisa de iniciar sessão para guardar favoritos!");
            window.location.href = "/Login";
        }
    } catch (error) {
        console.error("Erro:", error);
    }
}
