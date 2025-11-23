// =====================================================
// AGORA - JavaScript Utilities
// =====================================================

(function () {
    'use strict';

    // mobile Menu Toggle
    function initMobileMenu() {
        // create mobile menu toggle button
        const toggleBtn = document.createElement('button');
        toggleBtn.className = 'mobile-menu-toggle';
        toggleBtn.innerHTML = '<i class="bi bi-list"></i>';
        toggleBtn.setAttribute('aria-label', 'Toggle menu');
        document.body.appendChild(toggleBtn);

        const sidebar = document.querySelector('.sidebar');

        if (!sidebar) return;

        toggleBtn.addEventListener('click', function () {
            sidebar.classList.toggle('active');
            const icon = this.querySelector('i');

            if (sidebar.classList.contains('active')) {
                icon.className = 'bi bi-x-lg';
            } else {
                icon.className = 'bi bi-list';
            }
        });

        // close sidebar when clicking outside on mobile
        document.addEventListener('click', function (e) {
            if (window.innerWidth <= 768) {
                if (!sidebar.contains(e.target) && !toggleBtn.contains(e.target)) {
                    sidebar.classList.remove('active');
                    toggleBtn.querySelector('i').className = 'bi bi-list';
                }
            }
        });
    }

    // active link highlighting
    function highlightActiveLink() {
        const currentPath = window.location.pathname;
        const sidebarLinks = document.querySelectorAll('.sidebar-link');

        sidebarLinks.forEach(link => {
            link.classList.remove('active');
            const linkPath = new URL(link.href).pathname;

            if (linkPath === currentPath || (currentPath.startsWith(linkPath) && linkPath !== '/')) {
                link.classList.add('active');
            }
        });
    }

    // search functionality
    function initSearch() {
        const searchInput = document.querySelector('.search-box input');

        if (!searchInput) return;

        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                const query = this.value.trim();
                if (query) {
                    // redirect to search page
                    window.location.href = `/Search?q=${encodeURIComponent(query)}`;
                }
            }
        });
    }

    // smooth scroll
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const href = this.getAttribute('href');
                if (href !== '#' && href !== '#!') {
                    e.preventDefault();
                    const target = document.querySelector(href);
                    if (target) {
                        target.scrollIntoView({
                            behavior: 'smooth',
                            block: 'start'
                        });
                    }
                }
            });
        });
    }

    // notification badge update (example)
    function updateNotificationBadge() {
        // This would be called via SignalR or periodic polling in a real app
        // For now, it's just a placeholder
        const badge = document.querySelector('.notification-badge');
        if (badge) {
            // Example: badge.textContent = newNotificationCount;
        }
    }

    // form validation enhancement
    function enhanceFormValidation() {
        const forms = document.querySelectorAll('form');

        forms.forEach(form => {
            form.addEventListener('submit', function (e) {
                if (!this.checkValidity()) {
                    e.preventDefault();
                    e.stopPropagation();
                }
                this.classList.add('was-validated');
            });
        });
    }

    // fade in animation on scroll
    function initScrollAnimations() {
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver(function (entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('fade-in');
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        document.querySelectorAll('.card, .post-card, .group-card').forEach(el => {
            observer.observe(el);
        });
    }

    // auto-resize textarea
    function initAutoResizeTextarea() {
        document.querySelectorAll('textarea[data-autoresize]').forEach(textarea => {
            textarea.addEventListener('input', function () {
                this.style.height = 'auto';
                this.style.height = (this.scrollHeight) + 'px';
            });
        });
    }

    // image preview before upload
    function initImagePreview() {
        const imageInputs = document.querySelectorAll('input[type="file"][accept*="image"]');

        imageInputs.forEach(input => {
            input.addEventListener('change', function (e) {
                const file = e.target.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onload = function (event) {
                        // find or create preview element
                        let preview = input.nextElementSibling;
                        if (!preview || !preview.classList.contains('image-preview')) {
                            preview = document.createElement('div');
                            preview.className = 'image-preview mt-2';
                            input.parentNode.insertBefore(preview, input.nextSibling);
                        }
                        preview.innerHTML = `<img src="${event.target.result}" class="img-thumbnail" style="max-width: 300px;" alt="Preview">`;
                    };
                    reader.readAsDataURL(file);
                }
            });
        });
    }

    // toast notifications (bootstrap)
    function showToast(message, type = 'info') {
        const toastContainer = document.getElementById('toast-container') || createToastContainer();

        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type} border-0`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');

        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

        toastContainer.appendChild(toast);
        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();

        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    }

    function createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    }

    // initialize all features when DOM is ready
    function init() {
        initMobileMenu();
        highlightActiveLink();
        initSearch();
        initSmoothScroll();
        enhanceFormValidation();
        initScrollAnimations();
        initAutoResizeTextarea();
        initImagePreview();

        // update notification badge periodically (every 30 seconds)
        setInterval(updateNotificationBadge, 30000);
    }

    // run initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // expose showToast globally for use in other scripts
    window.Agora = {
        showToast: showToast
    };

})();