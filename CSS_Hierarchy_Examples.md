# CSS Hierarchy Implementation Examples

## Complete Working Example: Admin Approval Module

This example demonstrates the proper CSS hierarchy implementation for the Admin Approval module.

### 1. Base Layer (sb-admin-2.css) - DO NOT MODIFY
```css
/* From sb-admin-2.css - These are the base tokens we inherit */
.card {
    position: relative;
    display: flex;
    flex-direction: column;
    min-width: 0;
    word-wrap: break-word;
    background-color: #fff;
    background-clip: border-box;
    border: 1px solid #e3e6f0;
    border-radius: 0.35rem;
}

.btn-primary {
    color: #fff;
    background-color: #4e73df;
    border-color: #4e73df;
}

.table {
    width: 100%;
    margin-bottom: 1rem;
    color: #858796;
}
```

### 2. Site Layer (variables.css + site.css)

#### variables.css
```css
/* CSS Variables for Charter Branding */
:root {
    /* Charter-specific color palette */
    --charter-primary: #1e3a8a;      /* Deep blue */
    --charter-secondary: #3b82f6;    /* Bright blue */
    --charter-success: #10b981;      /* Green */
    --charter-warning: #f59e0b;      /* Amber */
    --charter-danger: #ef4444;       /* Red */
    --charter-info: #06b6d4;         /* Cyan */
    
    /* Status colors for approvals */
    --status-pending: #fbbf24;       /* Yellow */
    --status-approved: #10b981;      /* Green */
    --status-rejected: #ef4444;      /* Red */
    
    /* Spacing system */
    --spacing-xs: 0.25rem;           /* 4px */
    --spacing-sm: 0.5rem;            /* 8px */
    --spacing-md: 1rem;              /* 16px */
    --spacing-lg: 1.5rem;            /* 24px */
    --spacing-xl: 2rem;              /* 32px */
    
    /* Typography */
    --font-size-sm: 0.875rem;        /* 14px */
    --font-size-base: 1rem;          /* 16px */
    --font-size-lg: 1.125rem;        /* 18px */
    --font-size-xl: 1.25rem;         /* 20px */
    
    /* Shadows */
    --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
    --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
    --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
    
    /* Transitions */
    --transition-fast: 150ms ease-in-out;
    --transition-base: 250ms ease-in-out;
    --transition-slow: 350ms ease-in-out;
}

/* Dark mode support */
@media (prefers-color-scheme: dark) {
    :root {
        --charter-primary: #3b82f6;
        --charter-secondary: #60a5fa;
        /* ... other dark mode overrides */
    }
}
```

#### site.css
```css
/* Global overrides to SB Admin 2 */

/* Override primary button color */
.btn-primary {
    background-color: var(--charter-primary);
    border-color: var(--charter-primary);
    transition: all var(--transition-fast);
}

.btn-primary:hover {
    background-color: color-mix(in srgb, var(--charter-primary) 85%, black);
    border-color: color-mix(in srgb, var(--charter-primary) 85%, black);
    transform: translateY(-1px);
    box-shadow: var(--shadow-md);
}

/* Global card enhancements */
.card {
    box-shadow: var(--shadow-sm);
    transition: box-shadow var(--transition-base);
}

.card:hover {
    box-shadow: var(--shadow-md);
}

/* Global table styling */
.table {
    color: #5a5c69;
}

.table thead th {
    border-top: 0;
    border-bottom: 2px solid #e3e6f0;
    color: #5a5c69;
    font-weight: 600;
    text-transform: uppercase;
    font-size: var(--font-size-sm);
    letter-spacing: 0.05em;
}

/* Accessibility enhancements */
:focus-visible {
    outline: 2px solid var(--charter-primary);
    outline-offset: 2px;
}

/* Skip to main content link */
.skip-link {
    position: absolute;
    top: -40px;
    left: 0;
    background: var(--charter-primary);
    color: white;
    padding: var(--spacing-sm) var(--spacing-md);
    z-index: 100;
    text-decoration: none;
}

.skip-link:focus {
    top: 0;
}

/* Loading states */
.loading {
    position: relative;
    pointer-events: none;
    opacity: 0.7;
}

.loading::after {
    content: "";
    position: absolute;
    top: 50%;
    left: 50%;
    width: 20px;
    height: 20px;
    margin: -10px 0 0 -10px;
    border: 2px solid var(--charter-primary);
    border-right-color: transparent;
    border-radius: 50%;
    animation: spinner 0.75s linear infinite;
}

@keyframes spinner {
    to { transform: rotate(360deg); }
}
```

### 3. Module Layer (admin-approval.css)

```css
/* modules/admin-approval.css - Admin approval specific overrides */

/* Approval queue container */
.approval-queue {
    padding: var(--spacing-lg);
}

/* Override card for approval items */
.approval-card {
    border-left: 4px solid var(--status-pending);
    margin-bottom: var(--spacing-md);
    transition: all var(--transition-base);
}

.approval-card--approved {
    border-left-color: var(--status-approved);
    opacity: 0.8;
}

.approval-card--rejected {
    border-left-color: var(--status-rejected);
    opacity: 0.8;
}

/* Approval card header */
.approval-card__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: var(--spacing-md);
    background-color: #f8f9fc;
    border-bottom: 1px solid #e3e6f0;
}

.approval-card__title {
    font-size: var(--font-size-lg);
    font-weight: 600;
    color: var(--charter-primary);
    margin: 0;
}

.approval-card__status {
    display: inline-flex;
    align-items: center;
    padding: var(--spacing-xs) var(--spacing-sm);
    border-radius: 0.25rem;
    font-size: var(--font-size-sm);
    font-weight: 500;
}

.approval-card__status--pending {
    background-color: color-mix(in srgb, var(--status-pending) 20%, white);
    color: color-mix(in srgb, var(--status-pending) 80%, black);
}

/* Approval details */
.approval-details {
    padding: var(--spacing-md);
}

.approval-details__row {
    display: grid;
    grid-template-columns: 150px 1fr;
    gap: var(--spacing-md);
    margin-bottom: var(--spacing-sm);
}

.approval-details__label {
    font-weight: 600;
    color: #5a5c69;
}

.approval-details__value {
    color: #858796;
}

/* Action buttons */
.approval-actions {
    display: flex;
    gap: var(--spacing-sm);
    padding: var(--spacing-md);
    background-color: #f8f9fc;
    border-top: 1px solid #e3e6f0;
}

.btn-approve {
    background-color: var(--charter-success);
    border-color: var(--charter-success);
    color: white;
}

.btn-approve:hover {
    background-color: color-mix(in srgb, var(--charter-success) 85%, black);
    border-color: color-mix(in srgb, var(--charter-success) 85%, black);
}

.btn-reject {
    background-color: var(--charter-danger);
    border-color: var(--charter-danger);
    color: white;
}

.btn-reject:hover {
    background-color: color-mix(in srgb, var(--charter-danger) 85%, black);
    border-color: color-mix(in srgb, var(--charter-danger) 85%, black);
}

/* Approval table view (alternative layout) */
.approval-table {
    background: white;
    border-radius: 0.35rem;
    overflow: hidden;
    box-shadow: var(--shadow-sm);
}

.approval-table .table {
    margin-bottom: 0;
}

.approval-table tbody tr {
    transition: background-color var(--transition-fast);
}

.approval-table tbody tr:hover {
    background-color: #f8f9fc;
}

/* Status badges in table */
.status-badge {
    display: inline-block;
    padding: 0.25em 0.6em;
    font-size: 0.75rem;
    font-weight: 600;
    line-height: 1;
    text-align: center;
    white-space: nowrap;
    vertical-align: baseline;
    border-radius: 0.25rem;
}

.status-badge--pending {
    background-color: var(--status-pending);
    color: #000;
}

.status-badge--approved {
    background-color: var(--status-approved);
    color: #fff;
}

.status-badge--rejected {
    background-color: var(--status-rejected);
    color: #fff;
}

/* Mobile responsiveness */
@media (max-width: 768px) {
    .approval-queue {
        padding: var(--spacing-sm);
    }
    
    .approval-details__row {
        grid-template-columns: 1fr;
        gap: var(--spacing-xs);
    }
    
    .approval-details__label {
        font-size: var(--font-size-sm);
    }
    
    .approval-actions {
        flex-direction: column;
    }
    
    .approval-table {
        overflow-x: auto;
    }
    
    /* Hide less important columns on mobile */
    .hide-mobile {
        display: none;
    }
}

/* Loading state for approval actions */
.approval-card.is-processing {
    opacity: 0.6;
    pointer-events: none;
}

.approval-card.is-processing .approval-actions {
    position: relative;
}

.approval-card.is-processing .approval-actions::after {
    content: "Processing...";
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: white;
    padding: var(--spacing-sm) var(--spacing-md);
    border-radius: 0.25rem;
    box-shadow: var(--shadow-md);
    font-weight: 600;
    color: var(--charter-primary);
}

/* Accessibility improvements */
.approval-card:focus-within {
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--charter-primary) 25%, transparent);
}

/* High contrast mode support */
@media (prefers-contrast: high) {
    .approval-card {
        border-width: 2px;
        border-style: solid;
    }
    
    .status-badge {
        border: 1px solid currentColor;
    }
}
```

## Implementation in Razor View

### Approvals.cshtml
```html
@model ApprovalQueueViewModel
@{
    ViewData["Title"] = "Registration Approvals";
}

@section Styles {
    <link href="~/css/modules/admin-approval.css" rel="stylesheet" />
}

<div class="container-fluid approval-queue">
    <!-- Page Heading -->
    <div class="d-sm-flex align-items-center justify-content-between mb-4">
        <h1 class="h3 mb-0 text-gray-800">Registration Approvals</h1>
        <div>
            <span class="badge badge-primary">@Model.PendingCount Pending</span>
            <span class="badge badge-success">@Model.ApprovedTodayCount Approved Today</span>
        </div>
    </div>

    <!-- Approval Cards -->
    <div class="row">
        @foreach (var request in Model.PendingRequests)
        {
            <div class="col-12">
                <div class="card approval-card" data-request-id="@request.Id">
                    @Html.AntiForgeryToken()
                    <div class="approval-card__header">
                        <h2 class="approval-card__title">@request.FullName</h2>
                        <span class="approval-card__status approval-card__status--pending">
                            <i class="fas fa-clock mr-1"></i> Pending
                        </span>
                    </div>
                    
                    <div class="approval-details">
                        <div class="approval-details__row">
                            <span class="approval-details__label">Email:</span>
                            <span class="approval-details__value">@request.Email</span>
                        </div>
                        <div class="approval-details__row">
                            <span class="approval-details__label">Organization:</span>
                            <span class="approval-details__value">@request.Organization</span>
                        </div>
                        <div class="approval-details__row">
                            <span class="approval-details__label">Requested Role:</span>
                            <span class="approval-details__value">
                                <span class="badge badge-info">@request.RequestedRole</span>
                            </span>
                        </div>
                        <div class="approval-details__row">
                            <span class="approval-details__label">ID Number:</span>
                            <span class="approval-details__value">@request.IdNumber</span>
                        </div>
                        <div class="approval-details__row">
                            <span class="approval-details__label">Submitted:</span>
                            <span class="approval-details__value">
                                @request.CreatedAt.ToString("dd MMM yyyy HH:mm")
                            </span>
                        </div>
                    </div>
                    
                    <div class="approval-actions">
                        <button class="btn btn-approve" 
                                onclick="approveRequest('@request.Id')"
                                aria-label="Approve registration for @request.FullName">
                            <i class="fas fa-check mr-1"></i> Approve
                        </button>
                        <button class="btn btn-reject" 
                                onclick="showRejectModal('@request.Id')"
                                aria-label="Reject registration for @request.FullName">
                            <i class="fas fa-times mr-1"></i> Reject
                        </button>
                        <button class="btn btn-secondary" 
                                onclick="viewDetails('@request.Id')"
                                aria-label="View full details for @request.FullName">
                            <i class="fas fa-info-circle mr-1"></i> View Details
                        </button>
                    </div>
                </div>
            </div>
        }
    </div>

    @if (!Model.PendingRequests.Any())
    {
        <div class="alert alert-info" role="alert">
            <i class="fas fa-info-circle"></i> No pending registration requests at this time.
        </div>
    }
</div>

<!-- Rejection Modal -->
<div class="modal fade" id="rejectModal" tabindex="-1" role="dialog" aria-labelledby="rejectModalLabel" aria-hidden="true">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="rejectModalLabel">Reject Registration</h5>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
            <div class="modal-body">
                <form id="rejectForm">
                    <input type="hidden" id="rejectRequestId" />
                    <div class="form-group">
                        <label for="rejectionReason">Reason for Rejection <span class="text-danger">*</span></label>
                        <textarea class="form-control" id="rejectionReason" rows="3" required
                                  placeholder="Please provide a reason for rejection"></textarea>
                        <div class="invalid-feedback">
                            Rejection reason is required
                        </div>
                    </div>
                </form>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-danger" onclick="confirmReject()">
                    <i class="fas fa-times mr-1"></i> Confirm Rejection
                </button>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <script src="~/js/modules/admin-approval.js"></script>
}
```

## JavaScript Module (admin-approval.js)

```javascript
// modules/admin-approval.js

// Approval management functions
function approveRequest(requestId) {
    const card = document.querySelector(`[data-request-id="${requestId}"]`);
    card.classList.add('is-processing');
    
    fetch(`/Admin/ApproveRequest/${requestId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
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
            statusBadge.style.backgroundColor = 'var(--status-approved)';
            statusBadge.style.color = 'white';
            
            // Show success message
            showToast('success', `Registration approved for ${data.userName}`);
            
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

function showRejectModal(requestId) {
    document.getElementById('rejectRequestId').value = requestId;
    document.getElementById('rejectionReason').value = '';
    $('#rejectModal').modal('show');
}

function confirmReject() {
    const requestId = document.getElementById('rejectRequestId').value;
    const reason = document.getElementById('rejectionReason').value;
    
    if (!reason.trim()) {
        document.getElementById('rejectionReason').classList.add('is-invalid');
        return;
    }
    
    const card = document.querySelector(`[data-request-id="${requestId}"]`);
    card.classList.add('is-processing');
    $('#rejectModal').modal('hide');
    
    fetch(`/Admin/RejectRequest/${requestId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
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
            statusBadge.style.backgroundColor = 'var(--status-rejected)';
            statusBadge.style.color = 'white';
            
            // Show success message
            showToast('info', `Registration rejected for ${data.userName}`);
            
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
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} border-0" 
             role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="ml-2 mb-1 close text-white" data-dismiss="toast" aria-label="Close">
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
        toastContainer.style.position = 'fixed';
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

// View details function
function viewDetails(requestId) {
    window.location.href = `/Admin/RegistrationDetails/${requestId}`;
}

// Auto-refresh pending count
setInterval(() => {
    fetch('/Admin/GetPendingCount')
        .then(response => response.json())
        .then(data => {
            const badge = document.querySelector('.badge-primary');
            if (badge && data.count !== undefined) {
                badge.textContent = `${data.count} Pending`;
            }
        });
}, 60000); // Refresh every minute
```

## Key Benefits of This CSS Hierarchy

1. **Maintainability**: Clear separation of concerns between base theme, site customization, and module-specific styles
2. **Scalability**: Easy to add new modules without affecting existing styles
3. **Consistency**: Variables ensure consistent theming across all modules
4. **Performance**: Modular loading - only load CSS for active modules
5. **Override Control**: Predictable cascade order prevents style conflicts
6. **Accessibility**: Built-in support for high contrast and dark modes
7. **Responsiveness**: Mobile-first approach with proper breakpoints

## Testing the CSS Hierarchy

To verify the CSS hierarchy is working correctly:

1. **Inspect Element**: Check that styles are applied in the correct order
2. **Specificity**: Ensure module styles override site styles, which override base styles
3. **Variables**: Verify CSS variables are being used and can be changed globally
4. **Responsive**: Test all breakpoints to ensure mobile-first approach works
5. **Accessibility**: Use screen readers and keyboard navigation to test
6. **Performance**: Check that only necessary CSS files are loaded per page

This implementation ensures that the SB Admin 2 theme provides the foundation while allowing for complete customization at both the site and module levels.
