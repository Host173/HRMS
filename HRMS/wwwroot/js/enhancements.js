/**
 * HRMS Enhancements - Interactive Features
 * Handles modals, toasts, confirmations, and animations
 */

(function() {
    'use strict';

    // ========================================================================
    // TOAST NOTIFICATIONS
    // ========================================================================
    
    const Toast = {
        container: null,
        
        init() {
            if (!this.container) {
                this.container = document.createElement('div');
                this.container.className = 'toast-container';
                document.body.appendChild(this.container);
            }
        },
        
        show(options) {
            this.init();
            
            const {
                title = '',
                message = '',
                type = 'info', // success, error, warning, info
                duration = 5000,
                dismissible = true
            } = options;
            
            const toast = document.createElement('div');
            toast.className = `toast toast-${type}`;
            
            const icons = {
                success: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="color: var(--color-success)"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><path d="M22 4L12 14.01l-3-3"></path></svg>',
                error: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="color: var(--color-danger)"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>',
                warning: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="color: #f59e0b"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>',
                info: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="color: var(--color-primary)"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>'
            };
            
            toast.innerHTML = `
                <div class="toast-icon">${icons[type]}</div>
                <div class="toast-content">
                    ${title ? `<div class="toast-title">${title}</div>` : ''}
                    <div class="toast-message">${message}</div>
                </div>
                ${dismissible ? `
                    <button class="toast-close" aria-label="Close">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </button>
                ` : ''}
            `;
            
            this.container.appendChild(toast);
            
            // Close button handler
            if (dismissible) {
                const closeBtn = toast.querySelector('.toast-close');
                closeBtn.addEventListener('click', () => this.remove(toast));
            }
            
            // Auto dismiss
            if (duration > 0) {
                setTimeout(() => this.remove(toast), duration);
            }
            
            return toast;
        },
        
        remove(toast) {
            toast.classList.add('toast-exit');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        },
        
        success(message, title = 'Success') {
            return this.show({ title, message, type: 'success' });
        },
        
        error(message, title = 'Error') {
            return this.show({ title, message, type: 'error' });
        },
        
        warning(message, title = 'Warning') {
            return this.show({ title, message, type: 'warning' });
        },
        
        info(message, title = 'Info') {
            return this.show({ title, message, type: 'info' });
        }
    };
    
    // ========================================================================
    // MODAL SYSTEM
    // ========================================================================
    
    const Modal = {
        show(options) {
            const {
                title = '',
                body = '',
                buttons = [],
                size = 'medium', // small, medium, large
                closeOnBackdrop = true,
                closeButton = true
            } = options;
            
            const overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            
            const modal = document.createElement('div');
            modal.className = `modal modal-${size}`;
            
            let buttonsHTML = '';
            if (buttons.length > 0) {
                buttonsHTML = buttons.map(btn => `
                    <button class="btn-premium ${btn.className || 'btn-secondary'}" data-action="${btn.action || ''}">
                        ${btn.text}
                    </button>
                `).join('');
            }
            
            modal.innerHTML = `
                <div class="modal-header">
                    <h3 class="modal-title">${title}</h3>
                    ${closeButton ? '<button class="toast-close modal-close-btn" aria-label="Close" style="position: absolute; right: 20px; top: 20px;"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg></button>' : ''}
                </div>
                <div class="modal-body">${body}</div>
                ${buttonsHTML ? `<div class="modal-footer">${buttonsHTML}</div>` : ''}
            `;
            
            overlay.appendChild(modal);
            document.body.appendChild(overlay);
            
            // Prevent body scroll
            document.body.style.overflow = 'hidden';
            
            const close = () => {
                overlay.classList.add('toast-exit');
                setTimeout(() => {
                    if (overlay.parentNode) {
                        overlay.parentNode.removeChild(overlay);
                    }
                    document.body.style.overflow = '';
                }, 300);
            };
            
            // Close on backdrop click
            if (closeOnBackdrop) {
                overlay.addEventListener('click', (e) => {
                    if (e.target === overlay) close();
                });
            }
            
            // Close button
            if (closeButton) {
                const closeBtn = modal.querySelector('.modal-close-btn');
                if (closeBtn) {
                    closeBtn.addEventListener('click', close);
                }
            }
            
            // Button handlers
            buttons.forEach((btn, index) => {
                const btnElement = modal.querySelectorAll('[data-action]')[index];
                if (btnElement && btn.onClick) {
                    btnElement.addEventListener('click', (e) => {
                        btn.onClick(e, close);
                    });
                }
            });
            
            return { modal, overlay, close };
        },
        
        confirm(options) {
            const {
                title = 'Confirm Action',
                message = 'Are you sure you want to proceed?',
                confirmText = 'Confirm',
                cancelText = 'Cancel',
                type = 'danger', // danger, warning, info
                onConfirm = () => {},
                onCancel = () => {}
            } = options;
            
            const icons = {
                danger: '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>',
                warning: '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>',
                info: '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>'
            };
            
            const body = `
                <div class="confirm-dialog">
                    <div class="confirm-icon">
                        ${icons[type]}
                    </div>
                    <p style="font-size: 1rem; color: var(--color-text-primary); margin-bottom: 24px;">
                        ${message}
                    </p>
                </div>
            `;
            
            return this.show({
                title,
                body,
                closeButton: false,
                closeOnBackdrop: false,
                buttons: [
                    {
                        text: cancelText,
                        className: 'btn-secondary',
                        onClick: (e, close) => {
                            onCancel();
                            close();
                        }
                    },
                    {
                        text: confirmText,
                        className: type === 'danger' ? 'btn-danger' : 'btn-primary',
                        onClick: (e, close) => {
                            onConfirm();
                            close();
                        }
                    }
                ]
            });
        }
    };
    
    // ========================================================================
    // LOADING STATES
    // ========================================================================
    
    const Loading = {
        showButton(button) {
            if (button) {
                button.disabled = true;
                button.classList.add('btn-loading');
                button.dataset.originalText = button.innerHTML;
            }
        },
        
        hideButton(button) {
            if (button) {
                button.disabled = false;
                button.classList.remove('btn-loading');
                if (button.dataset.originalText) {
                    button.innerHTML = button.dataset.originalText;
                    delete button.dataset.originalText;
                }
            }
        },
        
        showSpinner(container) {
            if (container) {
                const spinner = document.createElement('div');
                spinner.className = 'loading-spinner';
                spinner.style.margin = '40px auto';
                container.innerHTML = '';
                container.appendChild(spinner);
            }
        }
    };
    
    // ========================================================================
    // FORM ENHANCEMENTS
    // ========================================================================
    
    function enhanceForms() {
        // Add loading state to submit buttons
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', function(e) {
                const submitBtn = this.querySelector('button[type="submit"]');
                if (submitBtn && !submitBtn.classList.contains('no-loading')) {
                    Loading.showButton(submitBtn);
                }
            });
        });
        
        // Animate validation errors
        document.querySelectorAll('.field-validation-error').forEach(error => {
            if (error.textContent.trim()) {
                error.classList.add('error-message');
                const input = error.previousElementSibling;
                if (input) {
                    input.classList.add('form-error');
                }
            }
        });
    }
    
    // ========================================================================
    // CARD ANIMATIONS
    // ========================================================================
    
    function animateCards() {
        const cards = document.querySelectorAll('.premium-card, .employee-card');
        cards.forEach((card, index) => {
            card.classList.add('animate-fade-in-scale');
            if (index < 6) {
                card.classList.add(`card-stagger-${index + 1}`);
            }
        });
    }
    
    // ========================================================================
    // CONFIRMATION DIALOGS FOR DELETE/REMOVE ACTIONS
    // ========================================================================
    
    function setupConfirmations() {
        // Add confirmation to all remove/delete buttons
        document.querySelectorAll('[data-confirm]').forEach(element => {
            element.addEventListener('click', function(e) {
                e.preventDefault();
                
                const message = this.dataset.confirm || 'Are you sure you want to proceed?';
                const type = this.dataset.confirmType || 'danger';
                
                Modal.confirm({
                    title: 'Confirm Action',
                    message: message,
                    type: type,
                    confirmText: 'Yes, proceed',
                    cancelText: 'Cancel',
                    onConfirm: () => {
                        // If it's a form, submit it
                        const form = this.closest('form');
                        if (form) {
                            form.submit();
                        } else if (this.href) {
                            window.location.href = this.href;
                        }
                    }
                });
            });
        });
    }
    
    // ========================================================================
    // CONVERT TEMPDATA MESSAGES TO TOASTS
    // ========================================================================
    
    function convertTempDataToToasts() {
        // Find existing success/error alerts and convert to toasts
        const successAlert = document.querySelector('[role="alert"]');
        if (successAlert && successAlert.textContent.includes('success')) {
            const message = successAlert.textContent.trim();
            successAlert.style.display = 'none';
            Toast.success(message);
        }
        
        const errorAlert = document.querySelector('.alert-danger, [role="alert"]');
        if (errorAlert && errorAlert.textContent.includes('error')) {
            const message = errorAlert.textContent.trim();
            errorAlert.style.display = 'none';
            Toast.error(message);
        }
    }
    
    // ========================================================================
    // INITIALIZE
    // ========================================================================
    
    function init() {
        enhanceForms();
        animateCards();
        setupConfirmations();
        
        // Add a small delay before converting TempData to avoid conflicts
        setTimeout(convertTempDataToToasts, 100);
    }
    
    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    
    // Expose globally
    window.HRMS = window.HRMS || {};
    window.HRMS.Toast = Toast;
    window.HRMS.Modal = Modal;
    window.HRMS.Loading = Loading;
    
})();

