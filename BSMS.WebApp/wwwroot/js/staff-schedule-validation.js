// Staff Schedule Client-side Validation
(function() {
    'use strict';

    // Constants matching backend rules
    const SHIFT_RULES = {
        MIN_HOURS: 4,
        MAX_HOURS: 12,
        START_HOUR: 6,
        END_HOUR: 22
    };

    // ✅ Auto-fill AssignedAt with current date/time
    function initializeAssignedAt() {
        const assignedAtInput = document.getElementById('assignedAtInput');
        if (assignedAtInput && !assignedAtInput.value) {
            const now = new Date();
            // Format: YYYY-MM-DDTHH:MM (datetime-local format)
            const localDateTime = new Date(now.getTime() - (now.getTimezoneOffset() * 60000))
                .toISOString()
                .slice(0, 16);
            assignedAtInput.value = localDateTime;
        }
    }

    // ✅ Toggle manual date editing
    function setupManualDateToggle() {
        const toggleBtn = document.getElementById('toggleManualDate');
        const assignedAtInput = document.getElementById('assignedAtInput');
        
        if (toggleBtn && assignedAtInput) {
            toggleBtn.addEventListener('click', function() {
                const isDisabled = assignedAtInput.disabled;
                assignedAtInput.disabled = !isDisabled;
                
                if (!isDisabled) {
                    // Re-lock: reset to current time
                    initializeAssignedAt();
                    toggleBtn.innerHTML = '<i class="bi bi-pencil"></i>';
                    toggleBtn.title = 'Enable manual edit';
                } else {
                    // Unlock: allow manual editing
                    toggleBtn.innerHTML = '<i class="bi bi-lock-fill"></i>';
                    toggleBtn.title = 'Lock to auto-fill';
                    assignedAtInput.focus();
                }
            });
        }
    }

    // ✅ Custom time range validator
    $.validator.addMethod('timerange', function(value, element, params) {
        if (!value) return true; // Let 'required' handle empty values
        
        const min = params.min || '00:00';
        const max = params.max || '23:59';
        
        return value >= min && value <= max;
    }, function(params, element) {
        return $(element).data('val-timerange') || 
               `Time must be between ${params.min} and ${params.max}`;
    });

    // ✅ Shift duration validator
    $.validator.addMethod('shiftduration', function(value, element) {
        const startInput = document.querySelector('input[name="Input.ShiftStart"]');
        const endInput = document.querySelector('input[name="Input.ShiftEnd"]');
        
        if (!startInput || !endInput || !startInput.value || !endInput.value) {
            return true; // Not enough data to validate
        }

        const start = parseTime(startInput.value);
        const end = parseTime(endInput.value);
        
        if (start >= end) {
            return false; // End must be after start
        }

        const durationHours = (end - start) / (1000 * 60 * 60);
        
        return durationHours >= SHIFT_RULES.MIN_HOURS && 
               durationHours <= SHIFT_RULES.MAX_HOURS;
    }, function() {
        return `Shift duration must be between ${SHIFT_RULES.MIN_HOURS} and ${SHIFT_RULES.MAX_HOURS} hours`;
    });

    // Helper: Parse time string to timestamp
    function parseTime(timeString) {
        const [hours, minutes] = timeString.split(':').map(Number);
        const date = new Date();
        date.setHours(hours, minutes, 0, 0);
        return date.getTime();
    }

    // ✅ Real-time shift validation
    function setupShiftValidation() {
        const startInput = document.querySelector('input[name="Input.ShiftStart"]');
        const endInput = document.querySelector('input[name="Input.ShiftEnd"]');
        
        if (!startInput || !endInput) return;

        const validateShift = () => {
            const start = startInput.value;
            const end = endInput.value;
            
            if (!start || !end) return;

            const startTime = parseTime(start);
            const endTime = parseTime(end);
            const durationHours = (endTime - startTime) / (1000 * 60 * 60);

            let errorMessage = '';
            let isValid = true;

            // Check order
            if (startTime >= endTime) {
                errorMessage = 'End time must be after start time';
                isValid = false;
            }
            // Check duration
            else if (durationHours < SHIFT_RULES.MIN_HOURS) {
                errorMessage = `Shift must be at least ${SHIFT_RULES.MIN_HOURS} hours`;
                isValid = false;
            }
            else if (durationHours > SHIFT_RULES.MAX_HOURS) {
                errorMessage = `Shift cannot exceed ${SHIFT_RULES.MAX_HOURS} hours`;
                isValid = false;
            }

            // Display feedback
            updateShiftFeedback(isValid, errorMessage, durationHours);
        };

        startInput.addEventListener('change', validateShift);
        endInput.addEventListener('change', validateShift);
    }

    function updateShiftFeedback(isValid, errorMessage, durationHours) {
        let feedbackDiv = document.getElementById('shiftDurationFeedback');
        
        if (!feedbackDiv) {
            feedbackDiv = document.createElement('div');
            feedbackDiv.id = 'shiftDurationFeedback';
            feedbackDiv.className = 'mt-2';
            const endInput = document.querySelector('input[name="Input.ShiftEnd"]');
            endInput.parentElement.appendChild(feedbackDiv);
        }

        if (!isValid) {
            feedbackDiv.innerHTML = `<span class="text-danger"><i class="bi bi-exclamation-circle me-1"></i>${errorMessage}</span>`;
        } else if (durationHours > 0) {
            feedbackDiv.innerHTML = `<span class="text-success"><i class="bi bi-check-circle me-1"></i>Shift duration: ${durationHours.toFixed(1)} hours</span>`;
        } else {
            feedbackDiv.innerHTML = '';
        }
    }

    // ✅ Unobtrusive validation adapters
    $.validator.unobtrusive.adapters.add('timerange', ['min', 'max'], function(options) {
        options.rules['timerange'] = {
            min: options.params.min,
            max: options.params.max
        };
        options.messages['timerange'] = options.message;
    });

    // ✅ Form submission with real-time validation
    function setupFormSubmission() {
        const form = document.getElementById('staffAssignmentForm');
        if (!form) return;

        $(form).on('submit', function(e) {
            const startInput = $('input[name="Input.ShiftStart"]');
            const endInput = $('input[name="Input.ShiftEnd"]');

            // Add shift duration validation
            startInput.rules('add', {
                shiftduration: true
            });
            endInput.rules('add', {
                shiftduration: true
            });

            // Let jQuery validation handle it
            if (!$(form).valid()) {
                e.preventDefault();
                showNotification('Please correct validation errors', 'danger');
            }
        });
    }

    // ✅ Show notification helper
    function showNotification(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        toast.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 5000);
    }

    // ✅ Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        initializeAssignedAt();
        setupManualDateToggle();
        setupShiftValidation();
        setupFormSubmission();
    });
})();