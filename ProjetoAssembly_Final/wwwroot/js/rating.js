async function submitRating(recipeId, stars) {
    console.log('[DEBUG JS] submitRating iniciado:', { recipeId, stars });

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : null;

    if (!token) {
        console.error("[DEBUG JS ERROR] Token de verificação NÃO encontrado. A submissão vai falhar (400).");
        return;
    }

    try {
        const url = `${window.location.pathname}?handler=RateOnly`;
        const payload = {
            recipeId: parseInt(recipeId),
            rating: parseInt(stars)
        };

        console.log('[DEBUG JS] Enviando Fetch para:', url, 'Payload:', payload);

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(payload)
        });

        console.log('[DEBUG JS] Resposta do Servidor Status:', response.status);

        if (response.ok) {
            const data = await response.json();
            console.log('[DEBUG JS] JSON recebido:', data);
            if (data.success) {
                console.log('[DEBUG JS] Sucesso! Recarregando página...');
                location.reload();
            } else {
                console.error("[DEBUG JS] Servidor retornou erro lógico:", data.message);
                alert("Erro: " + (data.message || "Não foi possível gravar a avaliação."));
            }
        } else {
            console.error("[DEBUG JS ERROR] Erro HTTP crítico:", response.status);
            alert("Erro na comunicação com o servidor.");
        }
    } catch (error) {
        console.error("[DEBUG JS EXCEPTION] Falha total no fetch:", error);
    }
}