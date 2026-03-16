/* darkmode.js */

/**
 * GESTÃO DE TEMA (DARK/LIGHT MODE)
 * Responsável por aplicar o tema e persistir a escolha no localStorage.
 */

const applyTheme = (theme) => {
    if (theme === "Dark") {
        document.body.classList.add('dark-mode');
    } else {
        document.body.classList.remove('dark-mode');
    }
    localStorage.setItem('theme', theme);
};

document.addEventListener('DOMContentLoaded', () => {
    const themeSelect = document.querySelector('select[name="InputTheme"]') || document.getElementById('InputTheme');
    const savedTheme = localStorage.getItem('theme');

    // Se já houver um tema guardado, aplica-o e ajusta o select
    if (savedTheme) {
        applyTheme(savedTheme);
        if (themeSelect) {
            themeSelect.value = savedTheme;
        }
    }

    else {
        applyTheme("Light");
        if (themeSelect) {
            themeSelect.value = "Light";
        }
    }

    // 3. Ouvir mudanças no select (específico da página de Settings)
    if (themeSelect) {
        themeSelect.addEventListener('change', (e) => {            
            const selectedTheme = e.target.value;
            applyTheme(selectedTheme);
        });
    }
});