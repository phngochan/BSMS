// SignalR handler for staff reservation updates
(function () {
    const body = document.body;
    if (!body || body.dataset.userRole !== "StationStaff") {
        return;
    }

    if (typeof signalR === "undefined") {
        console.warn("SignalR not loaded for staff reservation notifications.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

    connection.on("CREATE_RESERVATION", (data) => {
        updatePendingReservationList(data);
        showStaffReservationToast(data);
    });

    connection.onreconnecting((error) => {
        console.warn("Staff reservation hub reconnecting...", error);
    });

    connection.onclose((error) => {
        if (error) {
            console.error("Staff reservation hub disconnected:", error);
        }
    });

    startConnection();

    function startConnection() {
        connection.start()
            .then(() => console.log("Staff reservation SignalR connected"))
            .catch(err => {
                console.error("Staff reservation SignalR error:", err);
                setTimeout(startConnection, 5000);
            });
    }

    function updatePendingReservationList(data) {
        const list = document.getElementById("pendingReservationsList");
        const emptyState = document.getElementById("noReservationState");
        if (!list) {
            return;
        }

        if (emptyState) {
            emptyState.classList.add("d-none");
        }

        const existing = list.querySelector(`[data-reservation-id="${data.reservationId}"]`);
        const link = existing || document.createElement("a");
        link.className = "list-group-item list-group-item-action";
        link.dataset.reservationId = data.reservationId;
        link.href = `/Staff/ConfirmReservation?id=${data.reservationId}`;

        const scheduled = data.scheduledTime ? new Date(data.scheduledTime) : null;
        const formattedSchedule = scheduled
            ? `${scheduled.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} ${scheduled.toLocaleDateString()}`
            : "";

        link.innerHTML = `
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <h6 class="mb-1">#${data.reservationId}</h6>
                    <p class="mb-1 small">${data.userName || "Khách hàng"}</p>
                    <p class="mb-0 small text-muted">
                        <i class="bi bi-clock"></i> ${formattedSchedule}
                    </p>
                </div>
                <span class="badge bg-success">${data.status || "Active"}</span>
            </div>
        `;

        if (!existing) {
            list.prepend(link);
        }
    }

    function showStaffReservationToast(data) {
        const typeClass = "info";
        const toast = document.createElement("div");
        toast.className = `alert alert-${typeClass} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = "9999";

        const title = data?.userName ? `Khách: ${data.userName}` : "Đặt chỗ mới";
        const message = data?.message || "Có đặt chỗ mới cần xác nhận.";
        const schedule = data?.scheduledTime
            ? `<br/><small><i class="bi bi-clock"></i> ${new Date(data.scheduledTime).toLocaleString()}</small>`
            : "";

        toast.innerHTML = `
            <strong>${title}</strong><br/>
            ${message}
            ${schedule}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 6000);
    }
})();

