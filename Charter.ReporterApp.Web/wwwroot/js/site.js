// Charter Reporter App - Site-wide JavaScript

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize tooltips
    initializeTooltips();
    
    // Initialize form validation
    initializeFormValidation();
    
    // Initialize accessibility features
    initializeAccessibility();
    
    // Initialize loading states
    initializeLoadingStates();
});

// ========================================
// Tooltip Initialization
// ========================================
function initializeTooltips() {
    // Initialize Bootstrap tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// ========================================
// Form Validation
// ========================================
function initializeFormValidation() {
    // Add validation to all forms with class 'needs-validation'
    const forms = document.querySelectorAll('.needs-validation');
    
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
        }, false);
    });
}

// ========================================
// Accessibility Features
// ========================================
function initializeAccessibility() {
    // Handle skip link
    const skipLink = document.querySelector('.skip-link');
    if (skipLink) {
        skipLink.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.tabIndex = -1;
                target.focus();
            }
        });
    }
    
    // Ensure all images have alt text
    document.querySelectorAll('img:not([alt])').forEach(img => {
        console.warn('Image missing alt text:', img.src);
        img.setAttribute('alt', '');
    });
    
    // Add aria-current to active navigation items
    document.querySelectorAll('.nav-item.active > .nav-link').forEach(link => {
        link.setAttribute('aria-current', 'page');
    });
}

// ========================================
// Loading States
// ========================================
function initializeLoadingStates() {
    // Add loading class to forms on submit
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function() {
            if (form.checkValidity()) {
                const submitBtn = form.querySelector('[type="submit"]');
                if (submitBtn) {
                    submitBtn.classList.add('loading');
                    submitBtn.disabled = true;
                }
            }
        });
    });
}

// ========================================
// AJAX Helper Functions
// ========================================
const Ajax = {
    // GET request
    get: async function(url, options = {}) {
        return this.request('GET', url, null, options);
    },
    
    // POST request
    post: async function(url, data, options = {}) {
        return this.request('POST', url, data, options);
    },
    
    // PUT request
    put: async function(url, data, options = {}) {
        return this.request('PUT', url, data, options);
    },
    
    // DELETE request
    delete: async function(url, options = {}) {
        return this.request('DELETE', url, null, options);
    },
    
    // Generic request handler
    request: async function(method, url, data, options = {}) {
        const config = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                ...options.headers
            },
            ...options
        };
        
        // Add CSRF token if available
        const csrfToken = window.getCsrfToken();
        if (csrfToken) {
            config.headers['RequestVerificationToken'] = csrfToken;
        }
        
        // Add body if data is provided
        if (data && (method === 'POST' || method === 'PUT')) {
            config.body = JSON.stringify(data);
        }
        
        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            }
            
            return await response.text();
        } catch (error) {
            console.error('Ajax request failed:', error);
            throw error;
        }
    }
};

// ========================================
// Toast Notifications
// ========================================
const Toast = {
    success: function(message, title = 'Success') {
        this.show(message, title, 'success');
    },
    
    error: function(message, title = 'Error') {
        this.show(message, title, 'danger');
    },
    
    warning: function(message, title = 'Warning') {
        this.show(message, title, 'warning');
    },
    
    info: function(message, title = 'Info') {
        this.show(message, title, 'info');
    },
    
    show: function(message, title, type = 'info') {
        // Create toast container if it doesn't exist
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.style.position = 'fixed';
            container.style.top = '20px';
            container.style.right = '20px';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }
        
        // Create toast element
        const toastId = 'toast-' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-delay="5000">
                <div class="toast-header">
                    <strong class="mr-auto">${title}</strong>
                    <button type="button" class="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;
        
        // Add toast to container
        const toastWrapper = document.createElement('div');
        toastWrapper.innerHTML = toastHtml;
        container.appendChild(toastWrapper.firstElementChild);
        
        // Initialize and show toast
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
        
        // Remove toast after it's hidden
        toastElement.addEventListener('hidden.bs.toast', function() {
            toastElement.remove();
        });
    }
};

// ========================================
// Loading Overlay
// ========================================
const Loading = {
    show: function(message = 'Loading...') {
        // Remove existing overlay
        this.hide();
        
        // Create overlay
        const overlay = document.createElement('div');
        overlay.className = 'loading-overlay';
        overlay.id = 'global-loading-overlay';
        overlay.innerHTML = `
            <div class="text-center">
                <div class="loading-spinner"></div>
                <p class="mt-3">${message}</p>
            </div>
        `;
        
        document.body.appendChild(overlay);
    },
    
    hide: function() {
        const overlay = document.getElementById('global-loading-overlay');
        if (overlay) {
            overlay.remove();
        }
    }
};

// ========================================
// Utility Functions
// ========================================

// Format date
function formatDate(date, format = 'short') {
    const options = format === 'short' 
        ? { year: 'numeric', month: 'short', day: 'numeric' }
        : { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' };
    
    return new Date(date).toLocaleDateString('en-US', options);
}

// Format currency
function formatCurrency(amount, currency = 'USD') {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    }).format(amount);
}

// Format number
function formatNumber(number) {
    return new Intl.NumberFormat('en-US').format(number);
}

// Debounce function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ========================================
// Export utilities for use in other scripts
// ========================================
window.CharterApp = {
    Ajax: Ajax,
    Toast: Toast,
    Loading: Loading,
    formatDate: formatDate,
    formatCurrency: formatCurrency,
    formatNumber: formatNumber,
    debounce: debounce
};