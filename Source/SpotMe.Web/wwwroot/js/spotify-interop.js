// Spotify interop functions
window.SpotMe = window.SpotMe || {};

// Initialize logging
console.log("Loading SpotMe Spotify interop...");

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