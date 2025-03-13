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

// Setup a scroll event listener for infinite scrolling
window.setupScrollListener = function(elementId, dotnetRef) {
    const container = document.querySelector(`.${elementId}`);
    if (!container) {
        console.log(`Element with class ${elementId} not found for scroll listener`);
        return;
    }
    
    console.log(`Setting up scroll listener on ${elementId}`);
    
    // Get all scrollable parents
    const scrollables = [];
    let currentEl = container;
    
    // Check the element and all its parents for scrollability
    while (currentEl) {
        const style = window.getComputedStyle(currentEl);
        const overflowY = style.overflowY;
        
        if ((overflowY === 'auto' || overflowY === 'scroll') && 
            currentEl.scrollHeight > currentEl.clientHeight) {
            scrollables.push(currentEl);
        }
        
        currentEl = currentEl.parentElement;
    }
    
    console.log(`Found ${scrollables.length} scrollable parents to monitor`);
    
    // Force checking once even without scrolling to load if needed
    setTimeout(() => {
        console.log('Initial scroll check');
        dotnetRef.invokeMethodAsync('OnScroll');
    }, 500);
    
    // Throttle the scroll event to avoid too many calls
    let scrollTimeout;
    
    // Add scroll listener to all scrollable parents
    for (const scrollable of scrollables) {
        console.log(`Adding scroll listener to ${scrollable.className || 'element'}`);
        
        scrollable.addEventListener('scroll', function() {
            if (scrollTimeout) {
                clearTimeout(scrollTimeout);
            }
            
            scrollTimeout = setTimeout(function() {
                console.log('Scroll event fired, calling .NET method');
                dotnetRef.invokeMethodAsync('OnScroll');
            }, 200); // 200ms delay
        });
    }
    
    // Add scroll listener to window for good measure (in case of browser scrolling)
    window.addEventListener('scroll', function() {
        if (scrollTimeout) {
            clearTimeout(scrollTimeout);
        }
        
        scrollTimeout = setTimeout(function() {
            console.log('Window scroll event fired, calling .NET method');
            dotnetRef.invokeMethodAsync('OnScroll');
        }, 200); // 200ms delay
    });
    
    // Also check periodically in case the scroll event doesn't fire
    const intervalId = setInterval(function() {
        console.log('Periodic scroll check');
        dotnetRef.invokeMethodAsync('OnScroll');
    }, 2000); // Every 2 seconds
    
    // Store the interval ID for cleanup
    window.SpotMe.scrollCheckIntervalId = intervalId;
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