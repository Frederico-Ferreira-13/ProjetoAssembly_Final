function confirmDelete(id) {
    try {
        const style = getComputedStyle(document.body);
        const primaryGreen = style.getPropertyValue('--primary-green').trim() || '#28a745';
        const dangerRed = style.getPropertyValue('--danger-red').trim() || '#e74c3c';
        const swalBg = style.getPropertyValue('--swal-bg').trim() || '#ffffff';
        const swalText = style.getPropertyValue('--swal-text').trim() || '#333333';       

        Swal.fire({
            title: 'Eliminar Receita?',
            text: "Esta ação não pode ser revertida!",
            icon: 'warning',
            showCancelButton: true,
            background: swalBg,
            color: swalText,
            confirmButtonColor: dangerRed,
            cancelButtonColor: primaryGreen,
            confirmButtonText: '<i class="fa-solid fa-trash-can"></i> Sim, eliminar!',
            cancelButtonText: 'Cancelar',
            reverseButtons: true,
            borderRadius: '15px'
        }).then((result) => {
            if (result.isConfirmed) {                
                const form = document.getElementById(`deleteForm-${id}`);
                if (form) {                    
                    form.submit();
                } 
            }
        });

    } catch (error) {        
        // Fallback caso o Swal ou o CSS falhem
        if (confirm("Tem a certeza que deseja eliminar esta receita?")) {
            const form = document.getElementById(`deleteForm-${id}`);
            if (form) form.submit();
        }
    }
}