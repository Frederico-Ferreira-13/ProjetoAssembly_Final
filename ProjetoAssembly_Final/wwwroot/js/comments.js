function enableEditMode(commentId, currentText, currentRating) {
    console.log('[Comments] Ativando edição:', { commentId, currentText, currentRating });

    // Esconder visualização
    const displayEl = document.getElementById(`comment-display-${commentId}`);
    if (displayEl) {
        displayEl.style.display = 'none';
    }

    // Mostrar formulário de edição
    const editForm = document.getElementById(`comment-edit-${commentId}`);
    if (editForm) {
        editForm.style.display = 'block';

        // Focar no textarea e colocar cursor no fim
        const textarea = document.getElementById(`edit-text-${commentId}`);
        if (textarea) {
            textarea.focus();
            const len = textarea.value.length;
            textarea.setSelectionRange(len, len);
        }
    }
}

function cancelEdit(commentId) {
    console.log('[Comments] Cancelando edição:', commentId);

    // Mostrar visualização
    const displayEl = document.getElementById(`comment-display-${commentId}`);
    if (displayEl) {
        displayEl.style.display = 'block';
    }

    // Esconder formulário
    const editForm = document.getElementById(`comment-edit-${commentId}`);
    if (editForm) {
        editForm.style.display = 'none';
    }
}

async function deleteComment(commentId) {
    if (confirm('Tem a certeza que deseja eliminar este comentário')) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '?handler=DeleteComment';

        const commentInput = document.createElement('input');
        commentInput.type = 'hidden';
        commentInput.name = 'commentId';
        commentInput.value = commentId;

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = token;

        form.appendChild(commentInput);
        form.appendChild(tokenInput);
        document.body.appendChild(form);
        form.submit();
    }
}

function validateEditForm(commentId) {
    const textarea = document.getElementById(`edit-text-${commentId}`);
    if (!textarea) {
        console.error('[Comments] Textarea não encontrado:', commentId);
        return false;
    }

    const text = textarea.value.trim();

    // Validação de texto vazio
    if (text.length === 0) {
        alert('O comentário não pode estar vazio.');
        textarea.focus();
        return false;
    }

    // Validação de tamanho máximo
    if (text.length > 500) {
        alert('O comentário não pode exceder 500 caracteres.');
        textarea.focus();
        return false;
    }

    console.log('[Comments] Formulário válido, a submeter...');
    return true;
}

function initCharCounters() {
    document.addEventListener('input', function (e) {
        // Verificar se é um textarea de edição
        if (e.target && e.target.matches('textarea[id^="edit-text-"]')) {
            const match = e.target.id.match(/edit-text-(\d+)/);
            if (match) {
                const commentId = match[1];
                const count = e.target.value.length;
                const counter = document.getElementById(`char-count-${commentId}`);

                if (counter) {
                    counter.textContent = count;

                    // Mudar cor se próximo do limite
                    if (count > 450) {
                        counter.style.color = '#e74c3c';
                    } else if (count > 400) {
                        counter.style.color = '#f39c12';
                    } else {
                        counter.style.color = '';
                    }
                }
            }
        }
    });
}

function updateEditTimers() {
    const timers = document.querySelectorAll('.edit-timer');

    timers.forEach(timer => {
        const created = new Date(timer.dataset.created);
        const now = new Date();
        const elapsed = (now - created) / 1000; // segundos
        const remaining = 300 - elapsed; // 5 minutos = 300 segundos

        const timeDisplay = timer.querySelector('.time-remaining');

        if (remaining <= 0) {
            // Tempo expirado - remover botão de editar
            const actions = timer.closest('.comment-actions');
            if (actions) {
                actions.innerHTML = `
                    <span class="edit-expired">
                        <i class="fa-solid fa-clock"></i> 
                        Tempo de edição expirado
                    </span>
                `;
            }
        } else if (timeDisplay) {
            // Formatar tempo restante
            const minutes = Math.floor(remaining / 60);
            const seconds = Math.floor(remaining % 60);
            timeDisplay.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

            // Alerta visual se menos de 1 minuto
            if (remaining < 60) {
                timeDisplay.style.color = '#e74c3c';
                timeDisplay.style.fontWeight = 'bold';
            }
        }
    });
}

function initEditTimers() {
    // Executar imediatamente
    updateEditTimers();

    // Atualizar a cada segundo
    setInterval(updateEditTimers, 1000);
}

function initComments() {
    console.log('[Comments] Inicializando...');

    initCharCounters();
    initEditTimers();

    console.log('[Comments] Pronto!');
}

// Inicializar quando o DOM estiver pronto
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initComments);
} else {
    // DOM já carregado
    initComments();
}

function showReplyForm(commentId) {
    console.log('[Comments] Abrindo formulário de resposta para:', commentId);

    document.querySelectorAll('.reply-form-wrapper').forEach(el => {
        el.style.display = 'none';
    });

    const form = document.getElementById(`reply-form-container-${commentId}`);
    if (form) {
        form.style.display = 'block';

        const textarea = form.querySelector('textarea');
        if (textarea) textarea.focus();
    }
}

function hideReplyForm(commentId) {
    const form = document.getElementById(`reply-form-container-${commentId}`);
    if (form) {
        form.style.display = 'none';
    }
}

async function submitRating(recipeId, stars) {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : null;

    if (!token) {
        console.error("Token de verificação não encontrado.");
        return;
    }

    try {
        const url = `${window.location.pathname}?handler=RateOnly`;

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                // O Razor Pages procura este header específico
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                recipeId: parseInt(recipeId),
                rating: parseInt(stars)
            })
        });

        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                // Feedback visual antes de fazer reload (opcional)
                location.reload();
            } else {
                alert("Erro: " + (data.message || "Não foi possível gravar a avaliação."));
            }
        } else {
            console.error("Erro no servidor:", response.status);
            alert("Erro na comunicação com o servidor.");
        }
    } catch (error) {
        console.error("Erro na submissão:", error);
    }
}

// Exportar funções para uso global (necessário para os onclick inline)
window.enableEditMode = enableEditMode;
window.cancelEdit = cancelEdit;
window.validateEditForm = validateEditForm;
window.deleteComment = deleteComment;
window.showReplyForm = showReplyForm;
window.hideReplyForm = hideReplyForm;
window.submitRating = submitRating;
