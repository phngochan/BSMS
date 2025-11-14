// Staff Schedule Real-time Notifications
(function() {
    'use strict';

    if (typeof notificationConnection === 'undefined') {
        console.warn('SignalR connection not available');
        return;
    }

    // ✅ Listen for staff assignment changes
    notificationConnection.on("StaffAssignmentChanged", function(data) {
        console.log('Staff assignment changed:', data);

        // Show toast notification
        showStaffNotification(data);

        // Update table if on staff schedule page
        if (window.location.pathname.includes('/Admin/Operations/StaffSchedule')) {
            refreshAssignmentTable(data);
        }
    });

    function showStaffNotification(data) {
        const typeClass = data.action === 'removed' ? 'danger' : 
                         data.action === 'updated' ? 'info' : 'success';
        
        const icon = data.action === 'removed' ? 'bi-trash' :
                    data.action === 'updated' ? 'bi-pencil' : 'bi-check-circle';

        const toast = document.createElement('div');
        toast.className = `alert alert-${typeClass} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        toast.innerHTML = `
            <i class="bi ${icon} me-2"></i>
            <strong>Staff Schedule Update</strong><br>
            ${data.message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        document.body.appendChild(toast);

        setTimeout(() => toast.remove(), 6000);
    }

    function refreshAssignmentTable(data) {
        // Reload page to show updated data
        setTimeout(() => {
            window.location.reload();
        }, 2000);
    }

    console.log('Staff schedule notifications initialized');
})();