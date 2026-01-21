/* PROJETO: Receitas do Frederico
    DESCRIÇÃO: Lógica geral de navegação, slider, gestão de ingredientes e comunicação com a API.
*/

// --- 1. GESTÃO DE NAVEGAÇÃO (Active Link) ---
document.addEventListener('DOMContentLoaded', () => {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.container-nav ul li a');
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
            linha.querySelector('.btn-remover').onclick = () => {
                linha.remove();
            }
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

function toggleFavorito(event, id) {
    const btn = event.currentTarget;
    const icon = btn.querySelector('i');
    icon.classList.toggle('fa-regular');
    icon.classList.toggle('fa-solid');
    icon.style.color = icon.classList.contains('fa-solid') ? '#e4405f' : '';
    console.log("Receita favorita:", id);
}
