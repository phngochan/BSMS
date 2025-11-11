const reservationConnection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

reservationConnection.start()
    .then(() => {
        console.log("SignalR Reservation Hub connected");
    })
    .catch(err => {
        console.error("SignalR connection error:", err);
    });

reservationConnection.onreconnecting(error => {
    console.warn("SignalR reconnecting...", error);
    showReservationToast("Đang kết nối lại...", "warning");
});

reservationConnection.onreconnected(connectionId => {
    console.log("SignalR reconnected:", connectionId);
    showReservationToast("Đã kết nối lại thành công", "success");
});

reservationConnection.onclose(error => {
    console.error("SignalR connection closed:", error);
    showReservationToast("Mất kết nối realtime", "danger");
});

reservationConnection.on("CompleteReservation", (data) => {
    console.log("Reservation completed:", data);
    showReservationToast(data.message || "Đặt chỗ đã hoàn thành", data.type || "success");
    
    if (window.location.pathname.includes("/Driver/MyReservations") || 
        window.location.pathname.includes("/Driver/Index")) {
        setTimeout(() => location.reload(), 1500);
    }
});

reservationConnection.on("CancelReservation", (data) => {
    console.log("Reservation cancelled:", data);
    showReservationToast(data.message || "Đặt chỗ đã bị hủy", data.type || "warning");
    
    if (window.location.pathname.includes("/Driver/MyReservations") || 
        window.location.pathname.includes("/Driver/Index")) {
        setTimeout(() => location.reload(), 1500);
    }
});

reservationConnection.on("ReceiveNotification", (data) => {
    console.log("Notification:", data);
    showReservationToast(data.message, data.type || "info");
    
    if (data.message && (
        data.message.includes("đặt chỗ") || 
        data.message.includes("reservation") ||
        data.message.includes("Reservation")
    )) {
        if (window.location.pathname.includes("/Driver/MyReservations") || 
            window.location.pathname.includes("/Driver/Index")) {
            setTimeout(() => location.reload(), 1500);
        }
    }
});

function showReservationToast(message, type = "info") {
    const toast = document.createElement('div');
    toast.className = `alert alert-${getBootstrapAlertClass(type)} alert-dismissible fade show position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);';
    
    const icon = getIconForType(type);
    toast.innerHTML = `
        <i class="${icon} me-2"></i>
        <strong>${getTitleForType(type)}</strong><br>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    document.body.appendChild(toast);
    
    setTimeout(() => {
        if (toast.parentNode) {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }
    }, 5000);
}

function getBootstrapAlertClass(type) {
    const typeMap = {
        'success': 'success',
        'info': 'info',
        'warning': 'warning',
        'danger': 'danger',
        'error': 'danger'
    };
    return typeMap[type] || 'info';
}

function getIconForType(type) {
    const iconMap = {
        'success': 'bi bi-check-circle-fill',
        'info': 'bi bi-info-circle-fill',
        'warning': 'bi bi-exclamation-triangle-fill',
        'danger': 'bi bi-x-circle-fill',
        'error': 'bi bi-x-circle-fill'
    };
    return iconMap[type] || 'bi bi-info-circle-fill';
}

function getTitleForType(type) {
    const titleMap = {
        'success': 'Thành công',
        'info': 'Thông báo',
        'warning': 'Cảnh báo',
        'danger': 'Lỗi',
        'error': 'Lỗi'
    };
    return titleMap[type] || 'Thông báo';
}

window.reservationHub = {
    connection: reservationConnection,
    showToast: showReservationToast
};

