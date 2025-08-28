// modules/admin-approval.js - Admin approval functionality

// Approval management functions
function approveRequest(requestId) {
    const card = document.querySelector(`[data-request-id="${requestId}"]`);
    if (!card) return;
    
    card.classList.add('is-processing');
    
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    fetch(`/Admin/ApproveRequest/${requestId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update UI
            card.classList.remove('is-processing');
            card.classList.add('approval-card--approved');
            
            // Update status badge
            const statusBadge = card.querySelector('.approval-card__status');
            statusBadge.className = 'approval-card__status';
            statusBadge.innerHTML = '<i class="fas fa-check mr-1"></i> Approved';
            statusBadge.style.backgroundColor = 'var(--charter-success)';
            statusBadge.style.color = 'white';
            
            // Show success message
            showToast('success', `Registration approved for ${data.data.userName}`);
            
            // Update pending count
            updatePendingBadge(-1);
            
            // Remove card after animation
            setTimeout(() => {
                card.style.transition = 'all 0.5s ease-out';
                card.style.transform = 'translateX(100%)';
                card.style.opacity = '0';
                setTimeout(() => card.remove(), 500);
            }, 2000);
        } else {
            card.classList.remove('is-processing');
            showToast('error', data.message || 'Failed to approve registration');
        }
    })
    .catch(error => {
        card.classList.remove('is-processing');
        showToast('error', 'An error occurred while processing the request');
        console.error('Approval error:', error);
    });
}

function showRejectModal(requestId, userName) {
    document.getElementById('rejectRequestId').value = requestId;
    document.getElementById('rejectUserName').textContent = userName;
    document.getElementById('rejectionReason').value = '';
    
    // Remove validation classes
    const textarea = document.getElementById('rejectionReason');
    textarea.classList.remove('is-invalid');
    
    $('#rejectModal').modal('show');
    
    // Focus on textarea when modal is shown
    $('#rejectModal').on('shown.bs.modal', function() {
        textarea.focus();
    });
}

function confirmReject() {
    const requestId = document.getElementById('rejectRequestId').value;
    const reason = document.getElementById('rejectionReason').value.trim();
    const userName = document.getElementById('rejectUserName').textContent;
    
    if (!reason) {
        const textarea = document.getElementById('rejectionReason');
        textarea.classList.add('is-invalid');
        textarea.focus();
        return;
    }
    
    const card = document.querySelector(`[data-request-id="${requestId}"]`);
    if (!card) return;
    
    card.classList.add('is-processing');
    $('#rejectModal').modal('hide');
    
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    fetch(`/Admin/RejectRequest/${requestId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ reason: reason })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update UI
            card.classList.remove('is-processing');
            card.classList.add('approval-card--rejected');
            
            // Update status badge
            const statusBadge = card.querySelector('.approval-card__status');
            statusBadge.className = 'approval-card__status';
            statusBadge.innerHTML = '<i class="fas fa-times mr-1"></i> Rejected';
            statusBadge.style.backgroundColor = 'var(--charter-danger)';
            statusBadge.style.color = 'white';
            
            // Show success message
            showToast('info', `Registration rejected for ${userName}`);
            
            // Update pending count
            updatePendingBadge(-1);
            
            // Remove card after animation
            setTimeout(() => {
                card.style.transition = 'all 0.5s ease-out';
                card.style.transform = 'translateX(-100%)';
                card.style.opacity = '0';
                setTimeout(() => card.remove(), 500);
            }, 2000);
        } else {
            card.classList.remove('is-processing');
            showToast('error', data.message || 'Failed to reject registration');
        }
    })
    .catch(error => {
        card.classList.remove('is-processing');
        showToast('error', 'An error occurred while processing the request');
        console.error('Rejection error:', error);
    });
}

// Toast notification helper
function showToast(type, message) {
    // Remove any existing toasts
    const existingToasts = document.querySelectorAll('.toast');
    existingToasts.forEach(toast => toast.remove());
    
    const typeClass = type === 'success' ? 'bg-success' : 
                     type === 'error' ? 'bg-danger' : 
                     type === 'warning' ? 'bg-warning' : 'bg-info';
    
    const iconClass = type === 'success' ? 'fa-check-circle' : 
                     type === 'error' ? 'fa-exclamation-triangle' : 
                     type === 'warning' ? 'fa-exclamation-circle' : 'fa-info-circle';
    
    const toastHtml = `
        <div class="toast align-items-center text-white ${typeClass} border-0" 
             role="alert" aria-live="assertive" aria-atomic="true" 
             data-bs-delay="5000">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas ${iconClass} mr-2"></i>
                    ${message}
                </div>
                <button type="button" class="ml-2 mb-1 close text-white" data-bs-dismiss="toast" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        </div>
    `;
    
    // Add toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'position-fixed';
        toastContainer.style.top = '20px';
        toastContainer.style.right = '20px';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
    
    const toastElement = document.createElement('div');
    toastElement.innerHTML = toastHtml;
    toastContainer.appendChild(toastElement);
    
    const toast = new bootstrap.Toast(toastElement.firstElementChild);
    toast.show();
    
    // Remove after hidden
    toastElement.firstElementChild.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

// Update pending count badge
function updatePendingBadge(change) {
    const badge = document.querySelector('.badge-warning');
    if (badge) {
        const currentText = badge.textContent;
        const currentCount = parseInt(currentText.match(/\d+/)[0]);
        const newCount = Math.max(0, currentCount + change);
        badge.textContent = `${newCount} Pending`;
        
        // Update approved today count if approving
        if (change < 0) {
            const approvedBadge = document.querySelector('.badge-success');
            if (approvedBadge) {
                const approvedText = approvedBadge.textContent;
                const approvedCount = parseInt(approvedText.match(/\d+/)[0]);
                approvedBadge.textContent = `${approvedCount + 1} Approved Today`;
            }
        }
    }
}

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // ESC to close modal
    if (e.key === 'Escape') {
        const modal = document.getElementById('rejectModal');
        if (modal && modal.classList.contains('show')) {
            $('#rejectModal').modal('hide');
        }
    }
    
    // Enter to submit in rejection modal
    if (e.key === 'Enter' && e.target.id === 'rejectionReason') {
        if (e.ctrlKey || e.metaKey) {
            confirmReject();
        }
    }
});

// Real-time validation for rejection reason
document.addEventListener('DOMContentLoaded', function() {
    const rejectionTextarea = document.getElementById('rejectionReason');
    if (rejectionTextarea) {
        rejectionTextarea.addEventListener('input', function() {
            if (this.value.trim()) {
                this.classList.remove('is-invalid');
            }
        });
    }
});

// Auto-save rejection reason to localStorage (in case of accidental close)
document.addEventListener('DOMContentLoaded', function() {
    const rejectionTextarea = document.getElementById('rejectionReason');
    if (rejectionTextarea) {
        // Load saved reason
        const savedReason = localStorage.getItem('rejectionReason');
        if (savedReason) {
            rejectionTextarea.value = savedReason;
            localStorage.removeItem('rejectionReason');
        }
        
        // Save reason on input
        rejectionTextarea.addEventListener('input', function() {
            if (this.value.trim()) {
                localStorage.setItem('rejectionReason', this.value);
            } else {
                localStorage.removeItem('rejectionReason');
            }
        });
    }
});

// Clear saved reason when modal is hidden
$('#rejectModal').on('hidden.bs.modal', function() {
    localStorage.removeItem('rejectionReason');
});