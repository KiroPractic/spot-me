// InfiniteScroll Component JavaScript
(() => {
    // Store all active scroll handlers to avoid duplication
    const scrollHandlers = {};
    
    // Initial load status for each container
    const initialLoadStatus = {};
    
    // Loading status for each container
    const loadingStatus = {};
    
    // Store .NET object references
    const dotNetRefs = {};
    
    // Store last scroll positions to avoid duplicate triggers
    const lastScrollPositions = {};
    
    // Function to check if scrolling is near bottom
    function isNearBottom(container, percentage) {
        const rect = container.getBoundingClientRect();
        if (!rect.height) return false;
        
        // Get scrollable parent if the container itself isn't scrollable
        let scrollContainer = container;
        
        if (container.scrollHeight <= container.clientHeight) {
            // Container itself isn't scrollable, find a scrollable parent
            let parent = container.parentElement;
            while (parent) {
                const style = window.getComputedStyle(parent);
                const hasScroll = style.overflowY === 'auto' || style.overflowY === 'scroll';
                if (hasScroll && parent.scrollHeight > parent.clientHeight) {
                    scrollContainer = parent;
                    break;
                }
                parent = parent.parentElement;
            }
            
            // If no scrollable parent, use window
            if (scrollContainer === container) {
                // Check if element is near the bottom of the window
                const windowBottom = window.innerHeight;
                const elementBottom = rect.bottom;
                const threshold = windowBottom * (percentage / 100);
                return elementBottom <= windowBottom + threshold;
            }
        }
        
        // For scrollable containers
        const scrollTop = scrollContainer.scrollTop;
        const scrollHeight = scrollContainer.scrollHeight;
        const clientHeight = scrollContainer.clientHeight;
        
        if (scrollHeight <= clientHeight) return false;
        
        const distanceFromBottom = scrollHeight - (scrollTop + clientHeight);
        const triggerThreshold = scrollHeight * ((100 - percentage) / 100);
        
        return distanceFromBottom <= triggerThreshold;
    }
    
    // Function to handle scroll events
    function handleScroll(containerId, percentage) {
        if (loadingStatus[containerId]) return;
        
        const container = document.getElementById(containerId);
        if (!container) return;
        
        // Get current scroll position
        const position = window.scrollY;
        
        // Skip if we haven't moved much since last trigger
        if (lastScrollPositions[containerId] && 
            Math.abs(position - lastScrollPositions[containerId]) < 50) {
            return;
        }
        
        // Check if near bottom
        if (isNearBottom(container, percentage)) {
            // Update tracking variables
            lastScrollPositions[containerId] = position;
            loadingStatus[containerId] = true;
            
            // Invoke .NET method
            const dotNetRef = dotNetRefs[containerId];
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('LoadMoreItems')
                    .then(() => {
                        loadingStatus[containerId] = false;
                    })
                    .catch(error => {
                        console.error(`Error loading more items: ${error}`);
                        loadingStatus[containerId] = false;
                    });
            } else {
                loadingStatus[containerId] = false;
            }
        }
    }
    
    // Setup infinite scroll
    window.setupInfiniteScroll = function(containerId, dotNetRef, scrollPercentage = 80, loadInitial = true) {
        // Skip if already initialized
        if (scrollHandlers[containerId]) {
            console.log(`Infinite scroll already set up for ${containerId}`);
            
            // Update the reference in case it changed
            dotNetRefs[containerId] = dotNetRef;
            return;
        }
        
        console.log(`Setting up infinite scroll for ${containerId} with trigger at ${scrollPercentage}%`);
        
        // Store the .NET reference
        dotNetRefs[containerId] = dotNetRef;
        
        // Initial load status
        initialLoadStatus[containerId] = !loadInitial;
        
        // Create event handler
        const handler = () => handleScroll(containerId, scrollPercentage);
        
        // Store handler reference for cleanup
        scrollHandlers[containerId] = handler;
        
        // Add scroll event listener
        window.addEventListener('scroll', handler, { passive: true });
        
        // Trigger initial load if requested
        if (loadInitial) {
            setTimeout(() => {
                if (!initialLoadStatus[containerId]) {
                    initialLoadStatus[containerId] = true;
                    console.log(`Triggering initial load for ${containerId}`);
                    
                    loadingStatus[containerId] = true;
                    dotNetRef.invokeMethodAsync('LoadMoreItems')
                        .then(() => {
                            loadingStatus[containerId] = false;
                        })
                        .catch(error => {
                            console.error(`Error during initial load: ${error}`);
                            loadingStatus[containerId] = false;
                        });
                }
            }, 500);
        }
    };
    
    // Cleanup function
    window.cleanupInfiniteScroll = function(containerId) {
        const handler = scrollHandlers[containerId];
        if (handler) {
            console.log(`Cleaning up infinite scroll for ${containerId}`);
            
            // Remove event listener
            window.removeEventListener('scroll', handler);
            
            // Clean up references
            delete scrollHandlers[containerId];
            delete initialLoadStatus[containerId];
            delete loadingStatus[containerId];
            delete lastScrollPositions[containerId];
            delete dotNetRefs[containerId];
        }
    };
    
    // Save scroll position (for manual use if needed)
    window.saveScrollPosition = function() {
        return window.scrollY;
    };
    
    // Restore scroll position (for manual use if needed)
    window.restoreScrollPosition = function(position) {
        window.scrollTo(0, position);
    };
})();