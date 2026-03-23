/**
 * Lógica de Favoritos com Debugs - Projeto Assembly
 */
async function handleToggleFavorite(event, btn, recipeId) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    console.log(`%c ⚡ CLIQUE DETETADO: Receita ID ${recipeId}`, 'background: #222; color: #bada55');

    // 1. Verificar Token Antiforgery
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenElement?.value;

    if (!token) {
        console.error("❌ Erro: Token Antiforgery não encontrado!");
        return;
    }    

    try {

        btn.style.opacity = "0.7";
        btn.disabled = true;

        const response = await fetch(`${window.location.pathname}?handler=ToggleFavorite`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ recipeId: parseInt(recipeId) })
        });        

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Erro no servidor: ${errorText}`);
        }

        const data = await response.json();
        console.log("📥 Dados recebidos do servidor:", data);

        // 3. Atualizar UI
        const icon = btn.querySelector('i');
        const spanText = btn.querySelector('span');

        if (data.isFavorite) {
            btn.classList.add('active');
            if (icon) {
                icon.classList.replace('fa-regular', 'fa-solid');
            }
            if (spanText) spanText.textContent = "Favorito";
            btn.title = "Remover dos favoritos";
        } else {
            btn.classList.remove('active');
            if (icon) {
                icon.classList.replace('fa-solid', 'fa-regular');
            }
            if (spanText) spanText.textContent = "Favoritar";
            btn.title = "Adicionar aos favoritos";
        }

        // 3. ATUALIZAR O CONTADOR ESPECÍFICO (O "Pulo do Gato")
        // Isto procura o ID que tens no _RecipesCard: id="fav-count-@Model.RecipesId"
        const specificBadge = document.getElementById(`fav-count-${recipeId}`);

        if (specificBadge) {
            specificBadge.textContent = data.newCount;
            console.log(`✅ Contador da receita ${recipeId} atualizado para: ${data.newCount}`);
        }

    } catch (error) {
        console.error("🔥 Erro Crítico:", error);
        alert("Não foi possível atualizar o favorito. Tente novamente.");
    } finally {
        // Restaurar estado do botão
        btn.style.opacity = "1";
        btn.disabled = false;
    }
}