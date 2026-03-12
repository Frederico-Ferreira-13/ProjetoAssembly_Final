// --- GESTÃO DO PERFIL (Compatibilidade e Fecho) ---

function toggleMenu(event) {    
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    const dropdown = document.getElementById("profile-dropdown");
    if (dropdown) {
        dropdown.classList.toggle("show");
    }
}

document.addEventListener("click", function (event) {
    const profileMenu = document.querySelector(".profile-menu");
    if (profileMenu && !profileMenu.contains(event.target)) {
        const dropdown = document.getElementById("profile-dropdown");
        if (dropdown && dropdown.classList.contains("show")) {
            dropdown.classList.remove("show");            
        }
    }
});

const dropdown = document.getElementById("profile-dropdown");
if (dropdown) {
    dropdown.addEventListener("click", function (event) {
        event.stopPropagation();        
    });
}


document.addEventListener('DOMContentLoaded', () => {
    const btn = document.getElementById('profile-btn');
    if (btn) {
        btn.addEventListener('click', toggleMenu);
    }
    
});