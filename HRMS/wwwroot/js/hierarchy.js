// ==========================================================================
// ORGANIZATIONAL HIERARCHY - INTERACTIVE FUNCTIONALITY
// ==========================================================================

(function() {
    'use strict';

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initHierarchy();
    });

    function initHierarchy() {
        // Initialize search functionality
        initSearch();
        
        // Initialize expand/collapse functionality
        initExpandCollapse();
        
        // Initialize toggle buttons
        initToggleButtons();
        
        // Count total employees
        updateEmployeeCount();
        
        // Add smooth scroll behavior
        addSmoothScroll();
    }

    // ======================================================================
    // SEARCH FUNCTIONALITY
    // ======================================================================
    function initSearch() {
        const searchInput = document.getElementById('hierarchySearch');
        const clearBtn = document.getElementById('clearSearch');
        const orgChartContainer = document.getElementById('orgChartContainer');
        const noResults = document.getElementById('noResults');

        if (!searchInput) return;

        // Show/hide clear button
        searchInput.addEventListener('input', function() {
            if (this.value.trim().length > 0) {
                clearBtn.style.display = 'flex';
            } else {
                clearBtn.style.display = 'none';
            }
            performSearch(this.value.trim());
        });

        // Clear search
        clearBtn.addEventListener('click', function() {
            searchInput.value = '';
            clearBtn.style.display = 'none';
            performSearch('');
        });

        // Perform search
        function performSearch(query) {
            const nodes = document.querySelectorAll('.org-chart-node');
            let visibleCount = 0;
            const queryLower = query.toLowerCase();

            nodes.forEach(node => {
                const employeeName = node.getAttribute('data-employee-name') || '';
                const position = node.getAttribute('data-position') || '';
                const department = node.getAttribute('data-department') || '';

                const matches = query === '' || 
                    employeeName.includes(queryLower) ||
                    position.includes(queryLower) ||
                    department.includes(queryLower);

                if (matches) {
                    node.setAttribute('data-hidden', 'false');
                    node.setAttribute('data-highlight', query !== '' ? 'true' : 'false');
                    visibleCount++;
                    
                    // Show parent nodes if child matches
                    showParentNodes(node);
                } else {
                    // Check if any child matches
                    const hasMatchingChild = checkChildMatches(node, queryLower);
                    if (hasMatchingChild) {
                        node.setAttribute('data-hidden', 'false');
                        node.setAttribute('data-highlight', 'false');
                        visibleCount++;
                        showParentNodes(node);
                    } else {
                        node.setAttribute('data-hidden', 'true');
                        node.setAttribute('data-highlight', 'false');
                    }
                }
            });

            // Show/hide no results message
            if (visibleCount === 0 && query !== '') {
                if (orgChartContainer) orgChartContainer.style.display = 'none';
                if (noResults) noResults.style.display = 'flex';
            } else {
                if (orgChartContainer) orgChartContainer.style.display = 'block';
                if (noResults) noResults.style.display = 'none';
            }

            // Remove highlight after animation
            if (query !== '') {
                setTimeout(() => {
                    nodes.forEach(node => {
                        node.setAttribute('data-highlight', 'false');
                    });
                }, 2000);
            }
        }

        function checkChildMatches(node, query) {
            const children = node.querySelectorAll('.org-chart-node');
            for (let child of children) {
                const employeeName = child.getAttribute('data-employee-name') || '';
                const position = child.getAttribute('data-position') || '';
                const department = child.getAttribute('data-department') || '';

                if (employeeName.includes(query) || 
                    position.includes(query) || 
                    department.includes(query)) {
                    return true;
                }
                if (checkChildMatches(child, query)) {
                    return true;
                }
            }
            return false;
        }

        function showParentNodes(node) {
            let parent = node.parentElement;
            while (parent && !parent.classList.contains('org-chart-wrapper')) {
                if (parent.classList.contains('org-chart-child-wrapper')) {
                    const parentNode = parent.querySelector('.org-chart-node');
                    if (parentNode) {
                        parentNode.setAttribute('data-hidden', 'false');
                        // Expand parent's children container
                        const childrenContainer = parentNode.querySelector('.org-chart-children');
                        if (childrenContainer) {
                            childrenContainer.setAttribute('data-expanded', 'true');
                        }
                    }
                }
                parent = parent.parentElement;
            }
        }
    }

    // ======================================================================
    // EXPAND/COLLAPSE FUNCTIONALITY
    // ======================================================================
    function initExpandCollapse() {
        const expandAllBtn = document.getElementById('expandAll');
        const collapseAllBtn = document.getElementById('collapseAll');

        if (expandAllBtn) {
            expandAllBtn.addEventListener('click', function() {
                expandAll();
            });
        }

        if (collapseAllBtn) {
            collapseAllBtn.addEventListener('click', function() {
                collapseAll();
            });
        }
    }

    function expandAll() {
        const childrenContainers = document.querySelectorAll('.org-chart-children');
        childrenContainers.forEach(container => {
            container.setAttribute('data-expanded', 'true');
        });
        
        // Update toggle icons
        const toggleIcons = document.querySelectorAll('.org-toggle-icon');
        toggleIcons.forEach(icon => {
            icon.style.transform = 'rotate(0deg)';
        });
    }

    function collapseAll() {
        const childrenContainers = document.querySelectorAll('.org-chart-children');
        childrenContainers.forEach(container => {
            container.setAttribute('data-expanded', 'false');
        });
        
        // Update toggle icons
        const toggleIcons = document.querySelectorAll('.org-toggle-icon');
        toggleIcons.forEach(icon => {
            icon.style.transform = 'rotate(-90deg)';
        });
    }

    // ======================================================================
    // TOGGLE BUTTONS
    // ======================================================================
    function initToggleButtons() {
        const toggleButtons = document.querySelectorAll('.org-toggle-btn');
        
        toggleButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.stopPropagation();
                const card = this.closest('.org-card');
                const childrenContainer = card.parentElement.querySelector('.org-chart-children');
                const toggleIcon = this.querySelector('.org-toggle-icon');
                
                if (childrenContainer) {
                    const isExpanded = childrenContainer.getAttribute('data-expanded') === 'true';
                    
                    if (isExpanded) {
                        childrenContainer.setAttribute('data-expanded', 'false');
                        if (toggleIcon) toggleIcon.style.transform = 'rotate(-90deg)';
                        card.setAttribute('data-expanded', 'false');
                    } else {
                        childrenContainer.setAttribute('data-expanded', 'true');
                        if (toggleIcon) toggleIcon.style.transform = 'rotate(0deg)';
                        card.setAttribute('data-expanded', 'true');
                    }
                }
            });
        });

        // Also allow clicking on the card to toggle
        const orgCards = document.querySelectorAll('.org-card');
        orgCards.forEach(card => {
            card.addEventListener('click', function(e) {
                // Don't toggle if clicking on the toggle button
                if (e.target.closest('.org-toggle-btn')) return;
                
                const childrenContainer = this.parentElement.querySelector('.org-chart-children');
                const toggleBtn = this.querySelector('.org-toggle-btn');
                const toggleIcon = toggleBtn ? toggleBtn.querySelector('.org-toggle-icon') : null;
                
                if (childrenContainer && toggleBtn) {
                    const isExpanded = childrenContainer.getAttribute('data-expanded') === 'true';
                    
                    if (isExpanded) {
                        childrenContainer.setAttribute('data-expanded', 'false');
                        if (toggleIcon) toggleIcon.style.transform = 'rotate(-90deg)';
                        this.setAttribute('data-expanded', 'false');
                    } else {
                        childrenContainer.setAttribute('data-expanded', 'true');
                        if (toggleIcon) toggleIcon.style.transform = 'rotate(0deg)';
                        this.setAttribute('data-expanded', 'true');
                    }
                }
            });
        });
    }

    // ======================================================================
    // UPDATE EMPLOYEE COUNT
    // ======================================================================
    function updateEmployeeCount() {
        const totalEmployeesEl = document.getElementById('totalEmployees');
        if (!totalEmployeesEl) return;

        const nodes = document.querySelectorAll('.org-chart-node');
        const count = nodes.length;
        
        // Animate count
        animateValue(totalEmployeesEl, 0, count, 500);
    }

    function animateValue(element, start, end, duration) {
        let startTimestamp = null;
        const step = (timestamp) => {
            if (!startTimestamp) startTimestamp = timestamp;
            const progress = Math.min((timestamp - startTimestamp) / duration, 1);
            const value = Math.floor(progress * (end - start) + start);
            element.textContent = value;
            if (progress < 1) {
                window.requestAnimationFrame(step);
            }
        };
        window.requestAnimationFrame(step);
    }

    // ======================================================================
    // SMOOTH SCROLL
    // ======================================================================
    function addSmoothScroll() {
        const orgCards = document.querySelectorAll('.org-card');
        orgCards.forEach(card => {
            card.addEventListener('click', function() {
                // Smooth scroll to card if it's partially out of view
                const rect = this.getBoundingClientRect();
                const isVisible = rect.top >= 0 && rect.bottom <= window.innerHeight;
                
                if (!isVisible) {
                    this.scrollIntoView({
                        behavior: 'smooth',
                        block: 'center'
                    });
                }
            });
        });
    }

    // ======================================================================
    // KEYBOARD NAVIGATION
    // ======================================================================
    document.addEventListener('keydown', function(e) {
        // Escape key clears search
        if (e.key === 'Escape') {
            const searchInput = document.getElementById('hierarchySearch');
            const clearBtn = document.getElementById('clearSearch');
            if (searchInput && searchInput.value.trim().length > 0) {
                searchInput.value = '';
                if (clearBtn) clearBtn.style.display = 'none';
                const event = new Event('input');
                searchInput.dispatchEvent(event);
            }
        }
    });

})();
