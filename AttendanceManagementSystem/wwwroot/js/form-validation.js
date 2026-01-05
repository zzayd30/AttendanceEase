// Enhanced form validation and AJAX functionality
class FormValidator {
    constructor(formId, options = {}) {
        this.form = document.getElementById(formId);
        this.options = {
            validateOnBlur: true,
            validateOnInput: false,
            showPasswordStrength: false,
            apiEndpoint: '/api/auth',
            ...options
        };
        
        this.init();
    }

    init() {
        if (!this.form) return;

        // Add validation listeners
        if (this.options.validateOnBlur || this.options.validateOnInput) {
            this.addFieldValidators();
        }

        // Add password strength indicator
        if (this.options.showPasswordStrength) {
            this.addPasswordStrengthMeter();
        }

        // Add form submission handler
        this.form.addEventListener('submit', this.handleSubmit.bind(this));
    }

    addFieldValidators() {
        const fields = this.form.querySelectorAll('input[data-validate]');
        
        fields.forEach(field => {
            if (this.options.validateOnBlur) {
                field.addEventListener('blur', () => this.validateField(field));
            }
            
            if (this.options.validateOnInput) {
                field.addEventListener('input', debounce(() => this.validateField(field), 300));
            }

            // Special handling for email fields
            if (field.type === 'email') {
                field.addEventListener('blur', debounce(() => this.validateEmailExists(field), 500));
            }
        });
    }

    async validateField(field) {
        const value = field.value.trim();
        const validationType = field.getAttribute('data-validate');
        const errorContainer = this.getErrorContainer(field);
        
        // Clear previous errors
        this.clearFieldError(field);

        if (!value) {
            if (field.hasAttribute('required')) {
                this.showFieldError(field, `${this.getFieldLabel(field)} is required`);
                return false;
            }
            return true;
        }

        let isValid = true;
        let errorMessage = '';

        switch (validationType) {
            case 'email':
                isValid = this.isValidEmail(value);
                errorMessage = 'Please enter a valid email address';
                break;
            
            case 'password':
                const passwordResult = this.validatePassword(value);
                isValid = passwordResult.isValid;
                errorMessage = passwordResult.message;
                break;
            
            case 'confirmPassword':
                const originalPassword = this.form.querySelector('input[type="password"][data-validate="password"]');
                isValid = value === originalPassword?.value;
                errorMessage = 'Passwords do not match';
                break;
        }

        if (!isValid) {
            this.showFieldError(field, errorMessage);
        }

        return isValid;
    }

    async validateEmailExists(field) {
        const email = field.value.trim();
        
        if (!email || !this.isValidEmail(email)) return;

        try {
            const response = await fetch(`${this.options.apiEndpoint}/validate-email`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email })
            });

            const result = await response.json();
            
            if (result.success && result.data.exists) {
                // For registration forms, show warning if email exists
                if (this.form.id === 'register-form') {
                    this.showFieldWarning(field, 'This email is already registered');
                }
            }
        } catch (error) {
            console.error('Email validation error:', error);
        }
    }

    addPasswordStrengthMeter() {
        const passwordField = this.form.querySelector('input[type="password"][data-validate="password"]');
        if (!passwordField) return;

        // Create strength meter HTML
        const strengthMeter = document.createElement('div');
        strengthMeter.className = 'password-strength-meter mt-2';
        strengthMeter.innerHTML = `
            <div class="strength-bar">
                <div class="strength-fill"></div>
            </div>
            <div class="strength-text small text-muted"></div>
            <div class="strength-requirements small mt-1">
                <div class="requirement" data-requirement="minLength">
                    <i class="bi bi-x-circle text-danger"></i> At least 6 characters
                </div>
                <div class="requirement" data-requirement="hasUpper">
                    <i class="bi bi-x-circle text-danger"></i> One uppercase letter
                </div>
                <div class="requirement" data-requirement="hasLower">
                    <i class="bi bi-x-circle text-danger"></i> One lowercase letter
                </div>
                <div class="requirement" data-requirement="hasDigit">
                    <i class="bi bi-x-circle text-danger"></i> One number
                </div>
            </div>
        `;

        passwordField.parentNode.appendChild(strengthMeter);

        // Add input listener for strength checking
        passwordField.addEventListener('input', debounce(async (e) => {
            await this.updatePasswordStrength(e.target, strengthMeter);
        }, 200));
    }

    async updatePasswordStrength(passwordField, strengthMeter) {
        const password = passwordField.value;
        
        if (!password) {
            strengthMeter.style.display = 'none';
            return;
        }

        strengthMeter.style.display = 'block';

        try {
            const response = await fetch(`${this.options.apiEndpoint}/check-password-strength`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ password })
            });

            const result = await response.json();
            
            if (result.success) {
                this.updateStrengthDisplay(strengthMeter, result.data);
            }
        } catch (error) {
            console.error('Password strength check error:', error);
            // Fallback to client-side validation
            const localResult = this.validatePassword(password);
            this.updateStrengthDisplay(strengthMeter, localResult);
        }
    }

    updateStrengthDisplay(strengthMeter, data) {
        const fillElement = strengthMeter.querySelector('.strength-fill');
        const textElement = strengthMeter.querySelector('.strength-text');
        
        // Update strength bar
        const percentage = Math.min(data.score || 0, 100);
        fillElement.style.width = `${percentage}%`;
        
        // Update color based on strength
        const strength = data.strength || 'Very Weak';
        const colorClass = this.getStrengthColor(strength);
        fillElement.className = `strength-fill ${colorClass}`;
        
        // Update text
        textElement.textContent = `Password Strength: ${strength}`;
        textElement.className = `strength-text small ${colorClass}`;

        // Update requirements
        if (data.requirements) {
            const requirements = strengthMeter.querySelectorAll('.requirement');
            requirements.forEach(req => {
                const reqType = req.getAttribute('data-requirement');
                const icon = req.querySelector('i');
                const isValid = data.requirements[reqType];
                
                icon.className = isValid 
                    ? 'bi bi-check-circle text-success' 
                    : 'bi bi-x-circle text-danger';
            });
        }
    }

    getStrengthColor(strength) {
        switch (strength.toLowerCase()) {
            case 'strong': return 'text-success';
            case 'good': return 'text-info';
            case 'fair': return 'text-warning';
            case 'weak': return 'text-orange';
            default: return 'text-danger';
        }
    }

    validatePassword(password) {
        const requirements = {
            minLength: password.length >= 6,
            hasUpper: /[A-Z]/.test(password),
            hasLower: /[a-z]/.test(password),
            hasDigit: /\d/.test(password),
            hasSpecial: /[^a-zA-Z0-9]/.test(password)
        };

        const score = this.calculatePasswordScore(password, requirements);
        const isValid = requirements.minLength && requirements.hasUpper && 
                       requirements.hasLower && requirements.hasDigit;

        const strength = score >= 80 ? 'Strong' : 
                        score >= 60 ? 'Good' : 
                        score >= 40 ? 'Fair' : 
                        score >= 20 ? 'Weak' : 'Very Weak';

        return {
            isValid,
            score,
            strength,
            requirements,
            message: isValid ? '' : 'Password must meet the requirements'
        };
    }

    calculatePasswordScore(password, requirements) {
        let score = 0;
        
        if (requirements.minLength) score += 20;
        if (password.length >= 8) score += 10;
        if (password.length >= 12) score += 10;
        
        if (requirements.hasUpper) score += 15;
        if (requirements.hasLower) score += 15;
        if (requirements.hasDigit) score += 15;
        if (requirements.hasSpecial) score += 15;
        
        return score;
    }

    async handleSubmit(event) {
        event.preventDefault();
        
        // Show loading state
        this.setSubmitLoading(true);
        
        try {
            // Validate all fields
            const isValid = await this.validateAllFields();
            
            if (!isValid) {
                this.setSubmitLoading(false);
                return;
            }

            // Submit via AJAX if specified
            if (this.options.submitViaAjax) {
                await this.submitViaAjax();
            } else {
                // Allow normal form submission
                this.form.submit();
            }
        } catch (error) {
            console.error('Form submission error:', error);
            this.showFormError('An error occurred. Please try again.');
        } finally {
            this.setSubmitLoading(false);
        }
    }

    async validateAllFields() {
        const fields = this.form.querySelectorAll('input[data-validate]');
        let allValid = true;
        
        for (const field of fields) {
            const isValid = await this.validateField(field);
            if (!isValid) allValid = false;
        }
        
        return allValid;
    }

    async submitViaAjax() {
        const formData = new FormData(this.form);
        const jsonData = Object.fromEntries(formData.entries());

        const response = await fetch(this.form.action, {
            method: this.form.method || 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(jsonData)
        });

        const result = await response.json();
        
        if (result.success) {
            this.handleSubmitSuccess(result);
        } else {
            this.handleSubmitError(result);
        }
    }

    handleSubmitSuccess(result) {
        // Show success message
        this.showFormSuccess(result.message || 'Operation completed successfully');
        
        // Redirect if specified
        if (result.redirectUrl) {
            setTimeout(() => {
                window.location.href = result.redirectUrl;
            }, 1500);
        }
    }

    handleSubmitError(result) {
        if (result.errors && result.errors.length > 0) {
            result.errors.forEach(error => {
                this.showFormError(error);
            });
        } else {
            this.showFormError(result.message || 'An error occurred');
        }
    }

    // Utility methods
    isValidEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    getFieldLabel(field) {
        const label = this.form.querySelector(`label[for="${field.id}"]`);
        return label ? label.textContent.trim() : field.name || field.type;
    }

    getErrorContainer(field) {
        let container = field.parentNode.querySelector('.field-error');
        if (!container) {
            container = document.createElement('div');
            container.className = 'field-error text-danger small mt-1';
            field.parentNode.appendChild(container);
        }
        return container;
    }

    showFieldError(field, message) {
        const container = this.getErrorContainer(field);
        container.textContent = message;
        field.classList.add('is-invalid');
    }

    showFieldWarning(field, message) {
        const container = this.getErrorContainer(field);
        container.textContent = message;
        container.className = 'field-error text-warning small mt-1';
        field.classList.add('is-warning');
    }

    clearFieldError(field) {
        const container = field.parentNode.querySelector('.field-error');
        if (container) container.textContent = '';
        field.classList.remove('is-invalid', 'is-warning');
    }

    showFormError(message) {
        this.showFormMessage(message, 'danger');
    }

    showFormSuccess(message) {
        this.showFormMessage(message, 'success');
    }

    showFormMessage(message, type) {
        // Remove existing alerts
        const existingAlert = this.form.querySelector('.form-alert');
        if (existingAlert) existingAlert.remove();

        // Create new alert
        const alert = document.createElement('div');
        alert.className = `alert alert-${type} form-alert`;
        alert.innerHTML = `
            <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-triangle'} me-2"></i>
            ${message}
        `;

        this.form.insertBefore(alert, this.form.firstChild);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (alert.parentNode) alert.remove();
        }, 5000);
    }

    setSubmitLoading(loading) {
        const submitButton = this.form.querySelector('button[type="submit"]');
        if (!submitButton) return;

        if (loading) {
            submitButton.disabled = true;
            submitButton.classList.add('btn-loading');
            
            // Store original content
            if (!submitButton.hasAttribute('data-original-html')) {
                submitButton.setAttribute('data-original-html', submitButton.innerHTML);
            }
            
            submitButton.innerHTML = `
                <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                Processing...
            `;
        } else {
            submitButton.disabled = false;
            submitButton.classList.remove('btn-loading');
            
            const originalHtml = submitButton.getAttribute('data-original-html');
            if (originalHtml) {
                submitButton.innerHTML = originalHtml;
            }
        }
    }
}

// Debounce utility function
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

// Auto-initialize forms with validation
document.addEventListener('DOMContentLoaded', function() {
    // Login form
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
        new FormValidator('login-form', {
            validateOnBlur: true,
            submitViaAjax: false
        });
    }

    // Register form
    const registerForm = document.getElementById('register-form');
    if (registerForm) {
        new FormValidator('register-form', {
            validateOnBlur: true,
            showPasswordStrength: true,
            submitViaAjax: false
        });
    }

    // Forgot password form
    const forgotPasswordForm = document.getElementById('forgot-password-form');
    if (forgotPasswordForm) {
        new FormValidator('forgot-password-form', {
            validateOnBlur: true,
            submitViaAjax: false
        });
    }
});

// Additional utility for API calls
class ApiClient {
    static async call(endpoint, method = 'GET', data = null) {
        const config = {
            method,
            headers: {
                'Content-Type': 'application/json'
            }
        };

        if (data) {
            config.body = JSON.stringify(data);
        }

        try {
            const response = await fetch(endpoint, config);
            return await response.json();
        } catch (error) {
            console.error('API call error:', error);
            throw error;
        }
    }
}