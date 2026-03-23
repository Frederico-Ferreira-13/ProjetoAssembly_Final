console.log('[TESTE] ingredients.js carregado');

document.addEventListener('DOMContentLoaded', () => {
    console.log('[TESTE] DOM pronto');

    function initializeRows() {
        document.querySelectorAll('.ingredient-row').forEach(row => {
            const qtyInput = row.querySelector('.qty');
            const unitSelect = row.querySelector('.unit-select');
            const nameInput = row.querySelector('.ingredient-name');
            const detailInput = row.querySelector('.detail');
            const removeBtn = row.querySelector('.btn-remove');

            if (removeBtn) {
                removeBtn.addEventListener('click', () => {
                    console.log('[DEBUG] Removendo linha de ingrediente');
                    row.remove();
                });
            }

            if (qtyInput && unitSelect && nameInput && detailInput) {
                const updateFields = () => {
                    const hasQty = qtyInput.value.trim() !== '' && parseFloat(qtyInput.value) > 0;
                    unitSelect.disabled = !hasQty;
                    nameInput.disabled = !hasQty;
                    detailInput.disabled = !hasQty;

                    if (!hasQty) {
                        unitSelect.value = '';
                        nameInput.value = '';
                        detailInput.value = '';
                    }
                };

                // Executa na inicialização
                updateFields();

                // Atualiza ao digitar
                qtyInput.addEventListener('input', updateFields);
            }
        });
    }

    // Inicializa linhas existentes ao carregar
    initializeRows();

    // Botão de adicionar (existe tanto no create como no edit)
    const addBtn = document.getElementById('btn-add-ingredient');
    if (addBtn) {
        addBtn.addEventListener('click', (e) => {
            e.preventDefault();
            console.log('[DEBUG] Botão "Adicionar Ingrediente" clicado');

            const template = document.getElementById('ingredient-template');
            if (!template) {
                console.error('[ERRO] Template #ingredient-template não encontrado');
                return;
            }

            const clone = template.content.cloneNode(true);
            const row = clone.querySelector('.ingredient-row');

            if (!row) {
                console.error('[ERRO] .ingredient-row não encontrado no template');
                return;
            }

            // Remove 'disabled' de todos os inputs/selects do clone
            row.querySelectorAll('input, select').forEach(el => {
                el.disabled = false;
            });

            // Adiciona evento de remover ao botão de lixo do clone
            const removeBtn = row.querySelector('.btn-remove');
            if (removeBtn) {
                removeBtn.addEventListener('click', () => {
                    console.log('[DEBUG] Removendo linha clonada');
                    row.remove();
                });
            }

            // Foca no campo qty do novo ingrediente
            const qtyInput = row.querySelector('.qty');
            if (qtyInput) qtyInput.focus();

            // Adiciona o evento de input para desativar campos se qty vazio
            if (qtyInput) {
                const unitSelect = row.querySelector('.unit-select');
                const nameInput = row.querySelector('.ingredient-name');
                const detailInput = row.querySelector('.detail');

                const updateFields = () => {
                    const hasQty = qtyInput.value.trim() !== '' && parseFloat(qtyInput.value) > 0;
                    unitSelect.disabled = !hasQty;
                    nameInput.disabled = !hasQty;
                    detailInput.disabled = !hasQty;

                    if (!hasQty) {
                        unitSelect.value = '';
                        nameInput.value = '';
                        detailInput.value = '';
                    }
                };

                qtyInput.addEventListener('input', updateFields);
                // Executa uma vez para estado inicial
                updateFields();
            }

            // Adiciona a nova linha
            const list = document.getElementById('ingredient-list');
            if (list) {
                list.appendChild(row);
                console.log('[DEBUG] Novo ingrediente adicionado. Total atual:', list.querySelectorAll('.ingredient-row').length);
            } else {
                console.error('[ERRO] #ingredient-list não encontrado');
            }
        });
    } else {
        console.warn('[AVISO] Botão #btn-add-ingredient não encontrado nesta página');
    }

    console.log('[INGREDIENTES.JS] Inicialização concluída');
});