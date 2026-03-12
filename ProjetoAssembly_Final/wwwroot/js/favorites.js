/**
 * Lógica de Favoritos - Projeto Assembly
 */
async function handleToggleFavorite(event, btn, recipeId) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenElement?.value;

    if (!token) {
        console.error("Token Antiforgery não encontrado!");
        alert("Erro interno. Recarrega a página ou inicia sessão.");
        return;
    }

    console.log(`Tentar toggle favorito para receita ID: ${recipeId}`);

    try {

        const response = await fetch(`/recipes?handler=ToggleFavorite`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ recipeId: recipeId })
        });

        if (!response.ok) {
            if (response.status === 401) {
                alert("Inicia sessão para guardar favoritos!");
            } else {
                console.error("Erro no servidor:", response.status, await response.text());
                alert("Erro ao atualizar favorito.");
            }
            return;
        }

        const data = await response.json();
        console.log("Resposta JSON:", data);

        const icon = btn.querySelector('i');
        if (data.isFavorite) {
            btn.classList.add('active');
            icon?.classList.replace('fa-regular', 'fa-solid');
        } else {
            btn.classList.remove('active');
            icon?.classList.replace('fa-solid', 'fa-regular');
        }

        document.querySelectorAll('.favorite-badge span, .fav-badge').forEach(badge => {
            badge.textContent = data.newCount;
        });

        document.querySelectorAll(`button[data-recipe-id="${recipeId}"]`).forEach(otherBtn => {
            if (otherBtn !== btn) {
                const otherIcon = otherBtn.querySelector('i');
                if (data.isFavorite) {
                    otherBtn.classList.add('active');
                    otherIcon?.classList.replace('fa-regular', 'fa-solid');
                } else {
                    otherBtn.classList.remove('active');
                    otherIcon?.classList.replace('fa-solid', 'fa-regular');
                }
            }
        });

        console.log(`Favorito toggleado com sucesso: ${data.isFavorite ? 'adicionado' : 'removido'}`);
    } catch (error) {
        console.error("Erro completo na comunicação:", error);
        alert("Erro de conexão ou servidor. Verifica a internet ou tenta novamente mais tarde.");
    }
}