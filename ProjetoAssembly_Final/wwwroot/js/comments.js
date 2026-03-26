/**
 * Funções de Gestão de Comentários com Debug Ativo
 */

function showReplyForm(commentId) {
    console.log(`[DEBUG] A tentar abrir formulário de resposta para o ID: ${commentId}`);

    // 1. Verificar se encontra todos os wrappers para fechar
    const allWrappers = document.querySelectorAll('.reply-form-wrapper');
    console.log(`[DEBUG] Encontrados ${allWrappers.length} formulários de resposta na página.`);

    allWrappers.forEach(el => {
        el.style.display = 'none';
    });

    // 2. Tentar encontrar o elemento específico
    const targetId = `reply-form-container-${commentId}`;
    const form = document.getElementById(targetId);

    if (form) {
        console.log(`[DEBUG] Sucesso: Elemento ${targetId} encontrado. A mudar display para block.`);
        form.style.display = 'block';

        const textarea = form.querySelector('textarea');
        if (textarea) {
            console.log(`[DEBUG] Textarea encontrado. A dar foco...`);
            textarea.focus();
        }
    } else {
        console.error(`[ERRO CRÍTICO] Não encontrei nenhum elemento com o ID: ${targetId}`);
        console.warn(`[DICA] Verifica se no teu HTML o ID está escrito exatamente como: id="reply-form-container-${commentId}"`);
    }
}

function hideReplyForm(commentId) {
    console.log(`[DEBUG] A esconder formulário ${commentId}`);
    const form = document.getElementById(`reply-form-container-${commentId}`);
    if (form) form.style.display = 'none';
}

function enableEditMode(commentId) {
    console.log(`[DEBUG] Modo Edição iniciado para o comentário: ${commentId}`);

    const displayEl = document.getElementById(`comment-display-${commentId}`);
    const editForm = document.getElementById(`comment-edit-${commentId}`);
    const textarea = document.getElementById(`edit-text-${commentId}`);

    if (!displayEl) console.warn(`[DEBUG] Não encontrei o elemento de visualização: comment-display-${commentId}`);
    if (!editForm) console.warn(`[DEBUG] Não encontrei o formulário de edição: comment-edit-${commentId}`);
    if (!textarea) console.warn(`[DEBUG] Não encontrei o textarea: edit-text-${commentId}`);

    if (displayEl) displayEl.style.display = 'none';
    if (editForm) {
        editForm.style.display = 'block';
        if (textarea) {
            textarea.focus();
            const len = textarea.value.length;
            textarea.setSelectionRange(len, len);

            // Log do estado inicial da edição
            console.log(`[DEBUG] Edição aberta. Conteúdo atual: "${textarea.value.substring(0, 20)}..."`);
        }
    }
}

function cancelEdit(commentId) {
    console.log(`[DEBUG] Cancelar edição para: ${commentId}`);
    const displayEl = document.getElementById(`comment-display-${commentId}`);
    const editForm = document.getElementById(`comment-edit-${commentId}`);

    if (displayEl) displayEl.style.display = 'block';
    if (editForm) editForm.style.display = 'none';
}

function validateEditForm(commentId) {
    console.log(`[DEBUG] A validar submissão para ID: ${commentId}`);
    const textarea = document.getElementById(`edit-text-${commentId}`);

    if (!textarea) {
        console.error("[DEBUG] Erro de validação: Textarea não encontrado.");
        return false;
    }

    const text = textarea.value.trim();
    console.log(`[DEBUG] Texto a validar: "${text.substring(0, 30)}..." (Tamanho: ${text.length})`);

    if (text.length === 0) {
        alert('O comentário não pode estar vazio.');
        return false;
    }
    return true;
}

// Log de Inicialização
console.log("%c[SISTEMA] Script de Comentários carregado com sucesso.", "color: green; font-weight: bold;");