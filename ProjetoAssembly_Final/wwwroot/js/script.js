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

    // Gestão do Dark Mode (Definições)
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
    autoSlideInterval = setInterval(() => { pulsDivs(1); }, 5000);
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

// --- GESTÃO DO PERFIL (Compatibilidade e Fecho) ---

function toggleMenu() {
    const menu = document.getElementById("profile-dropdown");
    if (menu) {
        menu.classList.toggle("show");
    }
}

// Fechar menu ao clicar fora (útil para mobile onde o hover não existe)
window.onclick = function (event) {
    if (!event.target.closest('.profile-menu')) {
        const dropdown = document.getElementById("profile-dropdown");
        if (dropdown && dropdown.classList.contains('show')) {
            dropdown.classList.remove('show');
        }
    }
}

// --- COMUNICAÇÃO COM API / HANDLERS ---

// Lógica de Favoritos via AJAX
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
            const card = btn.closest('.recipe-card');

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
                        const grid = document.getElementById('recipe-container');
                        if (grid && grid.querySelectorAll('.recipe-card').length === 0) {
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
        console.error("Erro ao processar favorito:", error);
    }
}

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