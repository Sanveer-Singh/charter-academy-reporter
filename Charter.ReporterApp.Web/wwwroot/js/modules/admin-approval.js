// Charter Reporter App - Admin Approval Module

const AdminApproval = (function() {
    'use strict';
    
    // Module configuration
    const config = {
        endpoints: {
            approve: '/api/admin/approve/',
            reject: '/api/admin/reject/',
            getPending: '/api/admin/pending-count',
            getDetails: '/api/admin/registration-details/'
        },
        refreshInterval: 60000 // 1 minute
    };
    
    // Current state
    let currentRequests = [];
    let pendingCount = 0;
    
    // Initialize module
    const init = function() {
        // Initialize event handlers
        initializeEventHandlers();
        
        // Start auto-refresh
        startAutoRefresh();
        
        // Initialize search functionality
        initializeSearch();
        
        // Initialize filters
        initializeFilters();
    };
    
    // Initialize event handlers
    const initializeEventHandlers = function() {
        // Handle filter changes
        document.querySelectorAll('.approval-filters select').forEach(select => {
            select.addEventListener('change', applyFilters);
        });
        
        // Handle search input
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', CharterApp.debounce(applyFilters, 300));
        }
    };
    
    // Approve registration request
    const approveRequest = async function(requestId) {
        const card = document.querySelector(`[data-request-id="${requestId}"]`);
        if (!card) return;
        
        // Show confirmation
        const userName = card.querySelector('.approval-card__title').textContent;
        const confirmed = confirm(`Are you sure you want to approve the registration for ${userName}?`);
        if (!confirmed) return;
        
        // Add processing state
        card.classList.add('is-processing');
        
        try {
            const response = await CharterApp.Ajax.post(config.endpoints.approve + requestId, {
                requestVerificationToken: getCsrfToken()
            });
            
            if (response.success) {
                // Update UI
                handleApprovalSuccess(card, userName);
            } else {
                // Show error
                CharterApp.Toast.error(response.message || 'Failed to approve registration');
                card.classList.remove('is-processing');
            }
        } catch (error) {
            console.error('Approval error:', error);
            CharterApp.Toast.error('An error occurred while processing the approval');
            card.classList.remove('is-processing');
        }
    };
    
    // Handle successful approval
    const handleApprovalSuccess = function(card, userName) {
        // Update card status
        card.classList.remove('is-processing');
        card.classList.add('approval-card--approved');
        
        // Update status badge
        const statusBadge = card.querySelector('.approval-card__status');
        statusBadge.className = 'approval-card__status approval-card__status--approved';
        statusBadge.innerHTML = '<i class="fas fa-check mr-1"></i> Approved';
        
        // Show success message
        CharterApp.Toast.success(`Registration approved for ${userName}`);
        
        // Remove card after animation
        setTimeout(() => {
            card.style.transition = 'all 0.5s ease-out';
            card.style.transform = 'translateX(100%)';
            card.style.opacity = '0';
            
            setTimeout(() => {
                card.parentElement.remove();
                updatePendingCount(-1);
                checkEmptyState();
            }, 500);
        }, 1500);
    };
    
    // Show reject modal
    const showRejectModal = function(requestId) {
        const card = document.querySelector(`[data-request-id="${requestId}"]`);
        if (!card) return;
        
        const userName = card.querySelector('.approval-card__title').textContent;
        
        // Set request ID in modal
        document.getElementById('rejectRequestId').value = requestId;
        document.getElementById('rejectionReason').value = '';
        document.getElementById('rejectModalLabel').textContent = `Reject Registration - ${userName}`;
        
        // Clear any previous validation
        document.getElementById('rejectionReason').classList.remove('is-invalid');
        
        // Show modal
        $('#rejectModal').modal('show');
    };
    
    // Confirm rejection
    const confirmReject = async function() {
        const requestId = document.getElementById('rejectRequestId').value;
        const reason = document.getElementById('rejectionReason').value.trim();
        
        // Validate reason
        if (!reason) {
            document.getElementById('rejectionReason').classList.add('is-invalid');
            return;
        }
        
        const card = document.querySelector(`[data-request-id="${requestId}"]`);
        if (!card) return;
        
        // Hide modal and add processing state
        $('#rejectModal').modal('hide');
        card.classList.add('is-processing');
        
        try {
            const response = await CharterApp.Ajax.post(config.endpoints.reject + requestId, {
                reason: reason,
                requestVerificationToken: getCsrfToken()
            });
            
            if (response.success) {
                // Update UI
                handleRejectionSuccess(card, card.querySelector('.approval-card__title').textContent);
            } else {
                // Show error
                CharterApp.Toast.error(response.message || 'Failed to reject registration');
                card.classList.remove('is-processing');
            }
        } catch (error) {
            console.error('Rejection error:', error);
            CharterApp.Toast.error('An error occurred while processing the rejection');
            card.classList.remove('is-processing');
        }
    };
    
    // Handle successful rejection
    const handleRejectionSuccess = function(card, userName) {
        // Update card status
        card.classList.remove('is-processing');
        card.classList.add('approval-card--rejected');
        
        // Update status badge
        const statusBadge = card.querySelector('.approval-card__status');
        statusBadge.className = 'approval-card__status approval-card__status--rejected';
        statusBadge.innerHTML = '<i class="fas fa-times mr-1"></i> Rejected';
        
        // Show success message
        CharterApp.Toast.info(`Registration rejected for ${userName}`);
        
        // Remove card after animation
        setTimeout(() => {
            card.style.transition = 'all 0.5s ease-out';
            card.style.transform = 'translateX(-100%)';
            card.style.opacity = '0';
            
            setTimeout(() => {
                card.parentElement.remove();
                updatePendingCount(-1);
                checkEmptyState();
            }, 500);
        }, 1500);
    };
    
    // View registration details
    const viewDetails = function(requestId) {
        // In a real implementation, this would open a modal or navigate to a details page
        window.location.href = `/admin/registration-details/${requestId}`;
    };
    
    // Update pending count
    const updatePendingCount = function(change) {
        pendingCount += change;
        
        // Update all count badges
        document.querySelectorAll('#pendingCount, .approval-queue__stats .badge-warning').forEach(badge => {
            badge.textContent = `${pendingCount} Pending`;
        });
        
        // Update sidebar badge
        const sidebarBadge = document.querySelector('.nav-item a[href="/admin/approvals"] .badge-counter');
        if (sidebarBadge) {
            if (pendingCount > 0) {
                sidebarBadge.textContent = pendingCount;
                sidebarBadge.style.display = 'inline-block';
            } else {
                sidebarBadge.style.display = 'none';
            }
        }
    };
    
    // Check if empty state should be shown
    const checkEmptyState = function() {
        const cards = document.querySelectorAll('.approval-card:not(.approval-card--approved):not(.approval-card--rejected)');
        const emptyState = document.getElementById('emptyState');
        const cardsContainer = document.getElementById('approvalCards');
        
        if (cards.length === 0) {
            cardsContainer.style.display = 'none';
            emptyState.style.display = 'block';
        } else {
            cardsContainer.style.display = 'flex';
            emptyState.style.display = 'none';
        }
    };
    
    // Initialize search functionality
    const initializeSearch = function() {
        const searchInput = document.getElementById('searchInput');
        if (!searchInput) return;
        
        searchInput.addEventListener('input', CharterApp.debounce(function() {
            const searchTerm = this.value.toLowerCase();
            
            document.querySelectorAll('.approval-card').forEach(card => {
                const name = card.querySelector('.approval-card__title').textContent.toLowerCase();
                const email = card.querySelector('.approval-details__value').textContent.toLowerCase();
                const org = card.querySelectorAll('.approval-details__value')[1].textContent.toLowerCase();
                
                if (name.includes(searchTerm) || email.includes(searchTerm) || org.includes(searchTerm)) {
                    card.parentElement.style.display = 'block';
                } else {
                    card.parentElement.style.display = 'none';
                }
            });
        }, 300));
    };
    
    // Initialize filters
    const initializeFilters = function() {
        // Status filter
        const statusFilter = document.querySelector('.approval-filters select[value="pending"]');
        if (statusFilter) {
            statusFilter.addEventListener('change', function() {
                const status = this.value;
                
                document.querySelectorAll('.approval-card').forEach(card => {
                    if (!status) {
                        card.parentElement.style.display = 'block';
                    } else {
                        const cardStatus = card.classList.contains('approval-card--approved') ? 'approved' :
                                         card.classList.contains('approval-card--rejected') ? 'rejected' : 'pending';
                        
                        card.parentElement.style.display = cardStatus === status ? 'block' : 'none';
                    }
                });
            });
        }
        
        // Role filter
        const roleFilter = document.querySelector('.approval-filters select:last-child');
        if (roleFilter) {
            roleFilter.addEventListener('change', function() {
                const role = this.value;
                
                document.querySelectorAll('.approval-card').forEach(card => {
                    if (!role) {
                        card.parentElement.style.display = 'block';
                    } else {
                        const cardRole = card.querySelector('.badge-info').textContent;
                        card.parentElement.style.display = cardRole === role ? 'block' : 'none';
                    }
                });
            });
        }
    };
    
    // Apply all filters
    const applyFilters = function() {
        // This would combine all filter criteria in a real implementation
        // For now, trigger change events on filters
        document.querySelectorAll('.approval-filters select').forEach(select => {
            select.dispatchEvent(new Event('change'));
        });
    };
    
    // Auto-refresh pending count
    const startAutoRefresh = function() {
        // Initial count
        pendingCount = document.querySelectorAll('.approval-card:not(.approval-card--approved):not(.approval-card--rejected)').length;
        
        // Set up interval
        setInterval(async () => {
            try {
                const response = await CharterApp.Ajax.get(config.endpoints.getPending);
                if (response.count !== undefined && response.count !== pendingCount) {
                    // Reload page if count changed
                    window.location.reload();
                }
            } catch (error) {
                console.error('Failed to fetch pending count:', error);
            }
        }, config.refreshInterval);
    };
    
    // Get CSRF token
    const getCsrfToken = function() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    };
    
    // Public API
    return {
        init: init,
        approveRequest: approveRequest,
        showRejectModal: showRejectModal,
        confirmReject: confirmReject,
        viewDetails: viewDetails
    };
})();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    AdminApproval.init();
});

// Export for global access
window.AdminApproval = AdminApproval;