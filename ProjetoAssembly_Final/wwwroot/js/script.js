/* PROJETO: Receitas do Frederico
    DESCRIÇÃO: Lógica geral de navegação, slider, gestão de ingredientes e comunicação com a API.
*/

let slideIndex = 1;
let autoSlideInterval;

document.addEventListener('DOMContentLoaded', () => {

    // --- 1. GESTÃO DE NAVEGAÇÃO & TEMA ---
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-container ul li a');

    // Marcar link ativo no menu 
    navLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });    

    // Atualização do Sino de Notificações
    const pendingLink = document.getElementById('pendingBadgeLink');
    if (pendingLink) {
        updatePendingCount();
        setInterval(updatePendingCount, 60000); // Atualiza a cada minuto
    }

    // --- 2. PREVIEW DE IMAGEM (Receita ou Perfil) ---
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

    // --- 3. SLIDER AUTOMÁTICO ---
    const slides = document.getElementsByClassName("mySlides");
    if (slides.length > 0) {
        showDivs(slideIndex);
        setInterval(() => { plusDivs(1); }, 5000);
    }
});

// --- FUNÇÕES DO SLIDER (Ajustadas para reiniciar o tempo) ---

function startAutoSlide() {
    stopAutoSlide();
    autoSlideInterval = setInterval(() => { plusDivs(1); }, 5000);
}

function stopAutoSlide() {
    if (autoSlideInterval) clearInterval(autoSlideInterval);
}

function plusDivs(n) {
    showDivs(slideIndex += n);
    startAutoSlide();
}

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


// --- COMUNICAÇÃO COM API / HANDLERS ---

// Lógica de Favoritos via AJAX


async function updatePendingCount() {
    const badge = document.getElementById('pendingCount');
    if (!badge) return;

    try {
        // Chamada ao Handler OnGetCount na página PendingRecipes
        const response = await fetch('/PendingRecipes?handler=Count', {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const count = await response.json();
            badge.textContent = count;

            if (count > 0) {
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }  
    } catch (error) {
        console.error('Erro ao atualizar contador de pendentes:', error);
    }
}