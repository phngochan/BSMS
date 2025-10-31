// SignalR Connection Manager for BSMS
let notificationConnection = null;

// Initialize SignalR connection
function initializeSignalR() {
    notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Handle incoming notifications
    notificationConnection.on("ReceiveNotification", function (data) {
        showNotification(data.message, data.type);
        updateNotificationBadge();
    });

    // Handle battery status updates
    notificationConnection.on("BatteryStatusUpdated", function (data) {
        console.log("Battery updated:", data);
        updateBatteryUI(data);
    });

    // Handle swap transaction updates
    notificationConnection.on("SwapTransactionUpdate", function (data) {
        console.log("Swap transaction:", data);
        updateSwapUI(data);
    });

    // Connection state handlers
    notificationConnection.onreconnecting((error) => {
        console.log("SignalR reconnecting:", error);
        showConnectionStatus("Reconnecting...", "warning");
    });

    notificationConnection.onreconnected((connectionId) => {
        console.log("SignalR reconnected:", connectionId);
        showConnectionStatus("Connected", "success");
    });

    notificationConnection.onclose((error) => {
        console.log("SignalR disconnected:", error);
        showConnectionStatus("Disconnected", "danger");
    });

    // Start connection
    startConnection();
}

async function startConnection() {
    try {
        await notificationConnection.start();
        console.log("SignalR Connected");
        showConnectionStatus("Connected", "success");
    } catch (err) {
        console.error("SignalR Connection Error:", err);
        setTimeout(startConnection, 5000);
    }
}

// Show notification toast
function showNotification(message, type = "info") {
    const typeClass = {
        'info': 'alert-info',
        'success': 'alert-success',
        'warning': 'alert-warning',
        'danger': 'alert-danger',
        'error': 'alert-danger'
    }[type] || 'alert-info';

    const toast = document.createElement('div');
    toast.className = `alert ${typeClass} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
    toast.style.zIndex = '9999';
    toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(toast);

    setTimeout(() => toast.remove(), 5000);
}

// Update notification badge
function updateNotificationBadge() {
    const badge = document.querySelector('.btn-bell .dot');
    if (badge) {
        badge.style.display = 'block';
    }
}

// Update battery UI (example)
function updateBatteryUI(data) {
    const batteryRow = document.querySelector(`[data-battery-id="${data.batteryId}"]`);
    if (batteryRow) {
        // Update status badge and SoH
        const statusCell = batteryRow.querySelector('.battery-status');
        const sohCell = batteryRow.querySelector('.battery-soh');
        if (statusCell) statusCell.textContent = data.status;
        if (sohCell) sohCell.textContent = data.soh + '%';
    }
}

// Update swap UI (example)
function updateSwapUI(data) {
    // Refresh swap list or update specific row
    console.log("Update swap UI:", data);
}

// Show connection status
function showConnectionStatus(message, type) {
    console.log(`Connection: ${message} (${type})`);
}

// Auto-initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Only initialize if user is authenticated
    const isAuthenticated = document.querySelector('[data-user-authenticated="true"]');
    if (isAuthenticated) {
        initializeSignalR();
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (notificationConnection) {
        notificationConnection.stop();
    }
});
