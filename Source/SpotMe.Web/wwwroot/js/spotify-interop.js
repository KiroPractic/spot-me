// Spotify interop functions
window.SpotMe = window.SpotMe || {};

// Initialize logging
console.log("Loading SpotMe Spotify interop...");

// Function to check if a scroll container is near the bottom
window.isNearBottom = function(elementId) {
    const element = document.querySelector(`.${elementId}`);
    if (!element) {
        console.log(`Element with class ${elementId} not found`);
        return false;
    }
    
    // Get the parent element that's actually scrollable
    const scrollables = [];
    let currentEl = element;
    
    // Check the element and all its parents for scrollability
    while (currentEl) {
        // An element is scrollable if it has overflow auto/scroll and scrollHeight > clientHeight
        const style = window.getComputedStyle(currentEl);
        const overflowY = style.overflowY;
        
        if ((overflowY === 'auto' || overflowY === 'scroll') && 
            currentEl.scrollHeight > currentEl.clientHeight) {
            scrollables.push({
                element: currentEl,
                scrollTop: currentEl.scrollTop,
                clientHeight: currentEl.clientHeight,
                scrollHeight: currentEl.scrollHeight
            });
        }
        
        currentEl = currentEl.parentElement;
    }
    
    console.log('Found scrollable elements:', scrollables.length);
    
    // Check all scrollable parents - if any are near bottom, return true
    for (const scrollable of scrollables) {
        const threshold = 100; // px from bottom to trigger loading more
        const position = scrollable.scrollTop + scrollable.clientHeight;
        const height = scrollable.scrollHeight;
        
        console.log(`Scroll check - element: ${scrollable.element.className}, position: ${position}, height: ${height}, threshold: ${threshold}, diff: ${height - position}`);
        
        if (position + threshold >= height) {
            console.log('Found a scrollable that is near bottom!');
            return true;
        }
    }
    
    // If we checked all scrollables and none are near bottom
    return false;
};

// Setup scroll listener to auto-load more tracks when near bottom
window.setupTrackScrollListener = function(dotNetRef) {
    // Find the tracks table container
    const container = document.querySelector('.tracks-table-container');
    if (!container) {
        console.log('Tracks table container not found');
        return;
    }
    
    console.log('Setting up track scroll listener');
    
    // Add scroll event listener to the container
    container.addEventListener('scroll', function() {
        // Check if we're near the bottom
        const threshold = 100; // px from bottom to trigger loading
        const position = container.scrollTop + container.clientHeight;
        const height = container.scrollHeight;
        
        if (position + threshold >= height) {
            console.log('Scroll reached bottom, calling load more');
            dotNetRef.invokeMethodAsync('LoadMoreFromScroll');
        }
    });
    
    // Also listen for window scroll events
    window.addEventListener('scroll', function() {
        // Check if the table is visible in the viewport
        const rect = container.getBoundingClientRect();
        const isVisible = (
            rect.top >= 0 &&
            rect.left >= 0 &&
            rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
            rect.right <= (window.innerWidth || document.documentElement.clientWidth)
        );
        
        if (isVisible) {
            // Calculate if we're at the bottom
            const threshold = 100;
            const position = container.scrollTop + container.clientHeight;
            const height = container.scrollHeight;
            
            if (position + threshold >= height) {
                console.log('Window scroll reached bottom of tracks, calling load more');
                dotNetRef.invokeMethodAsync('LoadMoreFromScroll');
            }
        }
    });
};

// Setup a scroll event listener for infinite scrolling
window.setupScrollListener = function(elementId, dotnetRef) {
    // Find the scrollable container. Try multiple options.
    let scrollableContainer;
    
    // First try the specified element
    const specificContainer = document.querySelector(`.${elementId}`);
    if (specificContainer) {
        scrollableContainer = specificContainer;
        console.log(`Found specific container with class ${elementId}`);
    } else {
        // Look for any of these containers
        const potentialContainers = [
            '.tracks-table-container',
            '.playlist-tracks-container',
            '.playlist-detail-view',
            'main'
        ];
        
        for (const selector of potentialContainers) {
            const element = document.querySelector(selector);
            if (element) {
                const style = window.getComputedStyle(element);
                const hasScroll = (style.overflowY === 'auto' || style.overflowY === 'scroll');
                const isScrollable = element.scrollHeight > element.clientHeight;
                
                if (hasScroll && isScrollable) {
                    scrollableContainer = element;
                    console.log(`Found scrollable container: ${selector}`);
                    break;
                }
            }
        }
    }
    
    // If we still don't have a container, use the window
    if (!scrollableContainer) {
        console.log('No scrollable container found, using window as fallback');
        scrollableContainer = window;
    }
    
    console.log(`Setting up scroll listener on ${scrollableContainer === window ? 'window' : scrollableContainer.className || 'element'}`);
    
    // Store the last position to prevent continuous loading
    let lastPosition = 0;
    let isLoadingMore = false;
    
    // Function to check if we should load more content
    function shouldLoadMore(scrollable) {
        // Don't load more if already loading
        if (isLoadingMore) {
            console.log('Already loading more tracks, skipping');
            return false;
        }
        
        // Calculate how far we are from the bottom
        let position, total, bottom, scrollPercentage;
        
        if (scrollable === window) {
            // For window scrolling
            position = window.scrollY + window.innerHeight;
            total = document.documentElement.scrollHeight;
        } else {
            // For element scrolling
            position = scrollable.scrollTop + scrollable.clientHeight;
            total = scrollable.scrollHeight;
        }
        
        bottom = total - position;
        scrollPercentage = (position / total) * 100;
        
        console.log(`Scroll check - position: ${position}, total: ${total}, bottom: ${bottom}px, percentage: ${scrollPercentage.toFixed(1)}%`);
        
        // Only consider loading more if we're 80% through the content
        if (scrollPercentage > 80 && Math.abs(position - lastPosition) > 20) {
            console.log(`Near bottom: ${bottom}px from bottom, ${scrollPercentage.toFixed(1)}% scrolled`);
            lastPosition = position;
            return true;
        }
        
        return false;
    }
    
    // Load one batch initially if needed
    let initialLoadDone = false;
    setTimeout(() => {
        if (!initialLoadDone) {
            console.log('Initial content check');
            initialLoadDone = true;
            isLoadingMore = true;
            dotnetRef.invokeMethodAsync('OnScroll').then(() => {
                isLoadingMore = false;
            });
        }
    }, 500);
    
    // Throttle the scroll event to avoid too many calls
    let scrollTimeout;
    
    // Add scroll listener to the container element
    const scrollHandler = function() {
        if (scrollTimeout) {
            clearTimeout(scrollTimeout);
        }
        
        scrollTimeout = setTimeout(function() {
            if (shouldLoadMore(scrollableContainer)) {
                console.log('Loading more from scroll');
                isLoadingMore = true;
                dotnetRef.invokeMethodAsync('OnScroll').then(() => {
                    isLoadingMore = false;
                });
            }
        }, 200); // 200ms delay
    };
    
    // Add the scroll event listener
    if (scrollableContainer === window) {
        window.addEventListener('scroll', scrollHandler);
    } else {
        scrollableContainer.addEventListener('scroll', scrollHandler);
    }
    
    // Store scroll listener reference and cleanup info for disposal
    window.SpotMe.scrollData = {
        container: scrollableContainer,
        handler: scrollHandler
    };
};

// Cleanup scroll listener
window.cleanupScrollListener = function() {
    if (window.SpotMe && window.SpotMe.scrollData) {
        console.log('Cleaning up scroll listener');
        
        const { container, handler } = window.SpotMe.scrollData;
        
        if (container === window) {
            window.removeEventListener('scroll', handler);
        } else {
            container.removeEventListener('scroll', handler);
        }
        
        window.SpotMe.scrollData = null;
    }
};

// Function to save scroll position
window.saveScrollPosition = function(elementClass) {
    // Try to find element with the specific class
    let container = document.querySelector(`.${elementClass}`);
    
    // If not found, look for scrollable containers
    if (!container) {
        console.log(`Element with class ${elementClass} not found, trying to find scrollable container`);
        const scrollables = Array.from(document.querySelectorAll('.tracks-table-container, .playlist-tracks-container, main'));
        container = scrollables.find(el => {
            const style = window.getComputedStyle(el);
            return (style.overflowY === 'auto' || style.overflowY === 'scroll') && 
                   el.scrollHeight > el.clientHeight;
        });
    }
    
    if (!container) {
        console.log(`No suitable scrollable container found`);
        return 0;
    }
    
    console.log(`Saving scroll position from ${container.className}: ${container.scrollTop}`);
    return container.scrollTop;
};

// Function to restore scroll position
window.restoreScrollPosition = function(elementClass, position) {
    // Try to find element with the specific class
    let container = document.querySelector(`.${elementClass}`);
    
    // If not found, look for scrollable containers
    if (!container) {
        console.log(`Element with class ${elementClass} not found, trying to find scrollable container`);
        const scrollables = Array.from(document.querySelectorAll('.tracks-table-container, .playlist-tracks-container, main'));
        container = scrollables.find(el => {
            const style = window.getComputedStyle(el);
            return (style.overflowY === 'auto' || style.overflowY === 'scroll') && 
                   el.scrollHeight > el.clientHeight;
        });
    }
    
    if (!container) {
        console.log(`No suitable scrollable container found for restoring position`);
        return;
    }
    
    console.log(`Restoring scroll position to ${container.className}: ${position}`);
    container.scrollTop = position;
};

// Safe error handler
window.addEventListener('error', (event) => {
    console.error('Error caught by SpotMe:', event.error);
    return false;
});

// Safe script loading
window.SpotMe.loadScript = function(url) {
    return new Promise((resolve, reject) => {
        try {
            console.log(`Loading script: ${url}`);
            const script = document.createElement('script');
            script.src = url;
            script.async = true;
            script.defer = true;
            
            script.onload = () => {
                console.log(`Script loaded: ${url}`);
                resolve();
            };
            
            script.onerror = (error) => {
                console.error(`Script load error: ${url}`, error);
                reject(new Error(`Failed to load script: ${url}`));
            };
            
            document.body.appendChild(script);
        } catch (error) {
            console.error('Error loading script', error);
            reject(error);
        }
    });
};

// Load Spotify SDK safely
window.SpotMe.loadSpotifySDK = function() {
    if (!window.spotifyScriptLoaded) {
        window.spotifyScriptLoaded = true;
        return window.SpotMe.loadScript('https://sdk.scdn.co/spotify-player.js');
    } else {
        console.log('Spotify SDK already loaded');
        return Promise.resolve();
    }
};

// Initialize Spotify Web Playback SDK
window.SpotMe.initializePlayer = function(token) {
    return new Promise((resolve, reject) => {
        try {
            console.log('Initializing Spotify player...');
            
            if (typeof Spotify === 'undefined') {
                console.log('Spotify SDK not loaded yet, loading now...');
                window.SpotMe.loadSpotifySDK()
                    .then(() => {
                        console.log('Waiting for Spotify SDK to be ready...');
                        
                        // Set up handler for when SDK is ready
                        window.onSpotifyWebPlaybackSDKReady = () => {
                            console.log('Spotify SDK is ready, creating player...');
                            createPlayer(token, resolve, reject);
                        };
                    })
                    .catch(error => {
                        console.error('Failed to load Spotify SDK', error);
                        reject(error);
                    });
            } else {
                console.log('Spotify SDK already loaded, creating player...');
                createPlayer(token, resolve, reject);
            }
        } catch (error) {
            console.error('Error initializing Spotify player', error);
            reject(error);
        }
    });
};

// Helper to create the actual player
function createPlayer(token, resolve, reject) {
    try {
        const player = new Spotify.Player({
            name: 'SpotMe Player',
            getOAuthToken: callback => {
                console.log('Getting OAuth token...');
                callback(token);
            },
            volume: 0.5
        });
        
        // Error handling
        player.addListener('initialization_error', ({ message }) => {
            console.error('Initialization error:', message);
            reject(new Error(`Spotify initialization error: ${message}`));
        });
        
        player.addListener('authentication_error', ({ message }) => {
            console.error('Authentication error:', message);
            reject(new Error(`Spotify authentication error: ${message}`));
        });
        
        player.addListener('account_error', ({ message }) => {
            console.error('Account error:', message);
            reject(new Error(`Spotify account error: ${message}`));
        });
        
        player.addListener('playback_error', ({ message }) => {
            console.error('Playback error:', message);
        });
        
        // Ready
        player.addListener('ready', ({ device_id }) => {
            console.log('Player ready with device ID', device_id);
            window.SpotMe.deviceId = device_id;
            resolve(device_id);
        });
        
        // Not Ready
        player.addListener('not_ready', ({ device_id }) => {
            console.log('Device is not ready anymore', device_id);
        });
        
        // Connect the player
        console.log('Connecting to Spotify...');
        player.connect();
        
        // Store player reference
        window.SpotMe.player = player;
    } catch (error) {
        console.error('Error creating Spotify player', error);
        reject(error);
    }
}