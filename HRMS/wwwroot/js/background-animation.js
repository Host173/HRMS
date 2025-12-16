// Smooth Low-Poly Network Background Animation - Continuous across page navigations
(function() {
    'use strict';
    
    const STORAGE_KEY = 'bgAnimationStartTime';
    const duration = 40000; // 40 seconds in milliseconds
    const maxOffsetX = 20; // Maximum horizontal movement in pixels
    const maxOffsetY = 15; // Maximum vertical movement in pixels
    
    let animationId = null;
    let globalStartTime = null;
    
    // Easing function for smooth movement (ease-in-out)
    function easeInOutCubic(t) {
        return t < 0.5 
            ? 4 * t * t * t 
            : 1 - Math.pow(-2 * t + 2, 3) / 2;
    }
    
    // Get or initialize global start time from localStorage
    function getGlobalStartTime() {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
            const storedTime = parseInt(stored, 10);
            // Check if stored time is too old (more than 24 hours), reset if needed
            const now = Date.now();
            if (now - storedTime > 86400000) { // 24 hours
                localStorage.setItem(STORAGE_KEY, now.toString());
                return now;
            }
            return storedTime;
        } else {
            // First time - initialize
            const now = Date.now();
            localStorage.setItem(STORAGE_KEY, now.toString());
            return now;
        }
    }
    
    function animateBackground() {
        const now = Date.now();
        const elapsed = now - globalStartTime;
        
        // Calculate progress (continuous, never resets)
        const progress = (elapsed % duration) / duration;
        
        // Create smooth circular/elliptical movement pattern
        const easedProgress = easeInOutCubic(progress);
        
        // Calculate position using sine/cosine for smooth circular motion
        const angle = easedProgress * Math.PI * 2;
        const x = Math.cos(angle) * maxOffsetX;
        const y = Math.sin(angle * 0.7) * maxOffsetY; // Slightly elliptical
        
        // Update CSS custom properties for smooth transform
        document.documentElement.style.setProperty('--bg-offset-x', `${x}px`);
        document.documentElement.style.setProperty('--bg-offset-y', `${y}px`);
        
        animationId = requestAnimationFrame(animateBackground);
    }
    
    function startAnimation() {
        // Get the global start time (persists across page navigations)
        globalStartTime = getGlobalStartTime();
        
        // Initialize CSS variables
        document.documentElement.style.setProperty('--bg-offset-x', '0px');
        document.documentElement.style.setProperty('--bg-offset-y', '0px');
        
        // Start the animation loop
        animationId = requestAnimationFrame(animateBackground);
    }
    
    function init() {
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', startAnimation);
        } else {
            startAnimation();
        }
    }
    
    // Save current state before page unload (optional, for smoother transitions)
    window.addEventListener('beforeunload', function() {
        // The animation state is already persisted in localStorage
        // No need to do anything, it will continue seamlessly on next page
    });
    
    // Initialize when script loads
    init();
})();

