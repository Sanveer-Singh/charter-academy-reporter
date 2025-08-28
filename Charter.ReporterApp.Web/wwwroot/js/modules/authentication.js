// Charter Reporter App - Authentication Module JavaScript

// Authentication module namespace
const AuthModule = (function() {
    'use strict';
    
    // Module configuration
    const config = {
        endpoints: {
            login: '/api/account/login',
            register: '/api/account/register',
            forgotPassword: '/api/account/forgot-password',
            verifyEmail: '/api/account/verify-email',
            resendVerification: '/api/account/resend-verification'
        },
        redirects: {
            afterLogin: '/dashboard',
            afterLogout: '/account/login',
            afterRegister: '/account/login'
        }
    };
    
    // Password strength checker
    const checkPasswordStrength = function(password) {
        let strength = 0;
        const checks = {
            length: password.length >= 8,
            lowercase: /[a-z]/.test(password),
            uppercase: /[A-Z]/.test(password),
            numbers: /\d/.test(password),
            special: /[^A-Za-z0-9]/.test(password)
        };
        
        // Calculate strength
        Object.values(checks).forEach(passed => {
            if (passed) strength++;
        });
        
        // Return strength level
        if (strength < 2) return { level: 'weak', score: strength, checks };
        if (strength < 4) return { level: 'medium', score: strength, checks };
        return { level: 'strong', score: strength, checks };
    };
    
    // Email validation
    const validateEmail = function(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    };
    
    // ID number validation (South African format)
    const validateIdNumber = function(idNumber) {
        // Basic validation: 13 digits
        if (!/^\d{13}$/.test(idNumber)) {
            return false;
        }
        
        // Additional validation could include checksum verification
        return true;
    };
    
    // Phone number validation
    const validatePhoneNumber = function(phone) {
        // Remove spaces and special characters
        const cleaned = phone.replace(/\D/g, '');
        
        // Check if it's a valid length (10-15 digits)
        return cleaned.length >= 10 && cleaned.length <= 15;
    };
    
    // Handle login
    const handleLogin = async function(formData) {
        try {
            const response = await CharterApp.Ajax.post(config.endpoints.login, {
                email: formData.email,
                password: formData.password,
                rememberMe: formData.rememberMe || false
            });
            
            if (response.success) {
                // Store user data
                localStorage.setItem('user', JSON.stringify(response.user));
                
                // Show success message
                CharterApp.Toast.success('Login successful! Redirecting...');
                
                // Redirect after delay
                setTimeout(() => {
                    window.location.href = response.redirectUrl || config.redirects.afterLogin;
                }, 1000);
            } else {
                CharterApp.Toast.error(response.message || 'Invalid email or password');
            }
        } catch (error) {
            CharterApp.Toast.error('An error occurred during login. Please try again.');
            console.error('Login error:', error);
        }
    };
    
    // Handle registration
    const handleRegistration = async function(formData) {
        try {
            // Validate all fields
            const validation = validateRegistrationData(formData);
            if (!validation.isValid) {
                validation.errors.forEach(error => {
                    CharterApp.Toast.error(error);
                });
                return;
            }
            
            const response = await CharterApp.Ajax.post(config.endpoints.register, formData);
            
            if (response.success) {
                // Show success modal or redirect
                showRegistrationSuccess();
            } else {
                CharterApp.Toast.error(response.message || 'Registration failed');
                
                // Handle specific field errors
                if (response.fieldErrors) {
                    Object.entries(response.fieldErrors).forEach(([field, message]) => {
                        const input = document.querySelector(`[name="${field}"]`);
                        if (input) {
                            input.classList.add('is-invalid');
                            const feedback = input.nextElementSibling;
                            if (feedback && feedback.classList.contains('invalid-feedback')) {
                                feedback.textContent = message;
                            }
                        }
                    });
                }
            }
        } catch (error) {
            CharterApp.Toast.error('An error occurred during registration. Please try again.');
            console.error('Registration error:', error);
        }
    };
    
    // Validate registration data
    const validateRegistrationData = function(data) {
        const errors = [];
        
        // Full name validation
        if (!data.fullName || data.fullName.length < 2) {
            errors.push('Full name must be at least 2 characters');
        }
        
        // Email validation
        if (!validateEmail(data.email)) {
            errors.push('Please enter a valid email address');
        }
        
        // ID number validation
        if (!validateIdNumber(data.idNumber)) {
            errors.push('ID number must be exactly 13 digits');
        }
        
        // Phone validation
        if (!validatePhoneNumber(data.phoneNumber)) {
            errors.push('Please enter a valid phone number');
        }
        
        // Organization validation
        if (!data.organization || data.organization.length < 2) {
            errors.push('Organization name is required');
        }
        
        // Address validation
        if (!data.address || data.address.length < 10) {
            errors.push('Please enter a complete address');
        }
        
        // Role validation
        if (!data.requestedRole) {
            errors.push('Please select a role');
        }
        
        // Terms validation
        if (!data.terms) {
            errors.push('You must agree to the terms and conditions');
        }
        
        return {
            isValid: errors.length === 0,
            errors: errors
        };
    };
    
    // Show registration success
    const showRegistrationSuccess = function() {
        const modalHtml = `
            <div class="modal fade" id="registrationSuccessModal" tabindex="-1" role="dialog">
                <div class="modal-dialog modal-dialog-centered" role="document">
                    <div class="modal-content">
                        <div class="modal-body text-center p-5">
                            <div class="mb-4">
                                <i class="fas fa-check-circle text-success" style="font-size: 4rem;"></i>
                            </div>
                            <h2 class="h4 mb-3">Registration Successful!</h2>
                            <p class="mb-4">
                                Your registration has been submitted successfully. 
                                Please check your email for a verification link.
                            </p>
                            <p class="text-muted mb-4">
                                Once your email is verified, a Charter Admin will review and approve your account.
                            </p>
                            <a href="${config.redirects.afterRegister}" class="btn btn-primary">
                                Go to Login
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Add modal to body
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        
        // Show modal
        $('#registrationSuccessModal').modal('show');
        
        // Clean up after hide
        $('#registrationSuccessModal').on('hidden.bs.modal', function() {
            this.remove();
        });
    };
    
    // Handle forgot password
    const handleForgotPassword = async function(email) {
        try {
            if (!validateEmail(email)) {
                CharterApp.Toast.error('Please enter a valid email address');
                return;
            }
            
            const response = await CharterApp.Ajax.post(config.endpoints.forgotPassword, { email });
            
            if (response.success) {
                CharterApp.Toast.success('Password reset link sent to your email');
                
                // Show success message
                const form = document.querySelector('#forgotPasswordForm');
                if (form) {
                    form.innerHTML = `
                        <div class="text-center">
                            <i class="fas fa-envelope text-success mb-3" style="font-size: 3rem;"></i>
                            <h3 class="h5 mb-3">Check Your Email</h3>
                            <p class="text-muted">
                                We've sent a password reset link to ${email}. 
                                Please check your inbox and follow the instructions.
                            </p>
                            <a href="${config.redirects.afterLogout}" class="btn btn-primary mt-3">
                                Back to Login
                            </a>
                        </div>
                    `;
                }
            } else {
                CharterApp.Toast.error(response.message || 'Failed to send reset link');
            }
        } catch (error) {
            CharterApp.Toast.error('An error occurred. Please try again.');
            console.error('Forgot password error:', error);
        }
    };
    
    // Initialize password strength indicator
    const initPasswordStrength = function(passwordInput, indicatorElement) {
        if (!passwordInput || !indicatorElement) return;
        
        passwordInput.addEventListener('input', function() {
            const strength = checkPasswordStrength(this.value);
            
            // Update indicator
            indicatorElement.className = 'password-strength';
            indicatorElement.classList.add(`password-strength--${strength.level}`);
            
            // Update text
            const strengthText = indicatorElement.querySelector('.password-strength__text');
            if (strengthText) {
                strengthText.textContent = `Password strength: ${strength.level}`;
            }
            
            // Update progress bar
            const progressBar = indicatorElement.querySelector('.password-strength__bar');
            if (progressBar) {
                progressBar.style.width = `${(strength.score / 5) * 100}%`;
            }
        });
    };
    
    // Session management
    const checkSession = async function() {
        try {
            const response = await CharterApp.Ajax.get('/api/account/check-session');
            if (!response.isAuthenticated) {
                // Redirect to login if not authenticated
                window.location.href = config.redirects.afterLogout;
            }
        } catch (error) {
            console.error('Session check failed:', error);
        }
    };
    
    // Auto logout on inactivity
    let inactivityTimer;
    const inactivityTimeout = 20 * 60 * 1000; // 20 minutes
    
    const resetInactivityTimer = function() {
        clearTimeout(inactivityTimer);
        inactivityTimer = setTimeout(() => {
            CharterApp.Toast.warning('Your session has expired due to inactivity');
            setTimeout(() => {
                window.location.href = config.redirects.afterLogout;
            }, 3000);
        }, inactivityTimeout);
    };
    
    // Initialize inactivity detection
    const initInactivityDetection = function() {
        ['mousedown', 'keypress', 'scroll', 'touchstart'].forEach(event => {
            document.addEventListener(event, resetInactivityTimer, true);
        });
        resetInactivityTimer();
    };
    
    // Public API
    return {
        init: function() {
            // Initialize session management if on authenticated pages
            if (!window.location.pathname.includes('/account/')) {
                checkSession();
                initInactivityDetection();
            }
        },
        
        // Expose methods for use in forms
        handleLogin: handleLogin,
        handleRegistration: handleRegistration,
        handleForgotPassword: handleForgotPassword,
        checkPasswordStrength: checkPasswordStrength,
        initPasswordStrength: initPasswordStrength,
        
        // Validation methods
        validateEmail: validateEmail,
        validateIdNumber: validateIdNumber,
        validatePhoneNumber: validatePhoneNumber
    };
})();

// Initialize module when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    AuthModule.init();
});

// Export for use in other modules
window.AuthModule = AuthModule;