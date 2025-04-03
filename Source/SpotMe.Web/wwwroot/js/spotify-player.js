window.spotifyPlayer = {
    player: null,
    deviceId: null,
    dotNetReference: null,
    token: null,
    
    // Flag to track if SDK is ready
    sdkReady: false,
    
    // Register the Blazor instance
    registerDotNetInstance: function(dotNetRef) {
        console.log("Registering .NET instance");
        this.dotNetReference = dotNetRef;
    },
    
    // Callback when SDK is ready
    onSDKReady: function() {
        console.log("Spotify SDK is ready");
        this.sdkReady = true;
        
        // If we already have a token, create the player
        if (this.token && this.dotNetReference) {
            console.log("SDK ready and token available - creating player");
            this.createPlayer(this.token);
        } else {
            console.log("SDK ready but waiting for token or .NET reference");
        }
    },
    
    // Get the current device ID
    getDeviceId: function() {
        console.log("getDeviceId called, returning:", this.deviceId);
        return this.deviceId || "";
    },
    
    // Transfer playback to this device
    transferPlayback: async function() {
        console.log("Transferring playback to this device");
        if (!this.deviceId || !this.token) {
            console.error("Cannot transfer playback - missing device ID or token");
            console.log("Device ID:", this.deviceId);
            return false;
        }
        
        try {
            console.log("Making transfer request with device ID:", this.deviceId);
            
            const response = await fetch("https://api.spotify.com/v1/me/player", {
                method: "PUT",
                headers: {
                    "Authorization": `Bearer ${this.token}`,
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    device_ids: [this.deviceId],
                    play: true
                })
            });
            
            console.log("Transfer response status:", response.status);
            
            if (response.status >= 200 && response.status < 300) {
                console.log("Playback transferred successfully");
                return true;
            } else {
                let errorText = "Unknown error";
                try {
                    const errorData = await response.text();
                    console.error("Transfer error:", errorData);
                    errorText = errorData;
                } catch (e) {
                    console.error("Error getting error text:", e);
                }
                console.error(`Failed to transfer playback: ${response.status} - ${errorText}`);
                return false;
            }
        } catch (error) {
            console.error("Exception transferring playback:", error);
            return false;
        }
    },
    
    // Initialize the Spotify Web Player
    initialize: function(token, dotNetRef) {
        console.log("Initializing Spotify Web Player with token");
        this.token = token;
        
        // Ensure we have the dotNetReference
        if (dotNetRef) {
            this.dotNetReference = dotNetRef;
        }
        
        // If SDK is already ready, create player immediately
        if (window.Spotify && this.sdkReady) {
            console.log("SDK is already ready - creating player now");
            this.createPlayer(token);
        } else {
            console.log("SDK not ready yet - will create player when ready");
        }
    },
    
    // Create the Spotify player
    createPlayer: function(token) {
        console.log("Creating Spotify player instance with token: " + token.substring(0, 5) + "...");
        
        try {
            if (!window.Spotify) {
                console.error("Spotify SDK not loaded yet");
                if (this.dotNetReference) {
                    this.dotNetReference.invokeMethodAsync('OnPlayerError', "Spotify SDK not loaded yet");
                }
                return;
            }
            
            // Create the player
            this.player = new Spotify.Player({
                name: 'SpotMe Web Player',
                getOAuthToken: cb => { cb(token); },
                volume: 0.5
            });
            
            if (!this.player) {
                console.error("Failed to create Spotify player");
                return;
            }
            
            console.log("Player instance created");
            
            // Error handling
            this.player.addListener('initialization_error', ({ message }) => {
                console.error('Failed to initialize player:', message);
                if (this.dotNetReference) {
                    this.dotNetReference.invokeMethodAsync('OnPlayerError', message);
                }
            });
            
            this.player.addListener('authentication_error', ({ message }) => {
                console.error('Failed to authenticate:', message);
                if (this.dotNetReference) {
                    this.dotNetReference.invokeMethodAsync('OnPlayerError', message);
                }
            });
            
            this.player.addListener('account_error', ({ message }) => {
                console.error('Account error:', message);
                if (this.dotNetReference) {
                    this.dotNetReference.invokeMethodAsync('OnPlayerError', message);
                }
            });
            
            this.player.addListener('playback_error', ({ message }) => {
                console.error('Playback error:', message);
                if (this.dotNetReference) {
                    this.dotNetReference.invokeMethodAsync('OnPlayerError', message);
                }
            });
            
            // Playback status updates
            this.player.addListener('player_state_changed', state => {
                if (!state) {
                    console.log("Received empty player state");
                    return;
                }
                
                console.log('Player state changed', state);
                
                const isPlaying = !state.paused;
                let trackName = 'Not Playing';
                let artistName = 'No Artist';
                let imageUrl = '';
                
                if (state.track_window.current_track) {
                    trackName = state.track_window.current_track.name;
                    artistName = state.track_window.current_track.artists.map(artist => artist.name).join(', ');
                    
                    if (state.track_window.current_track.album && 
                        state.track_window.current_track.album.images && 
                        state.track_window.current_track.album.images.length > 0) {
                        imageUrl = state.track_window.current_track.album.images[0].url;
                    }
                }
                
                if (this.dotNetReference) {
                    console.log(`Updating playback state: ${isPlaying ? 'Playing' : 'Paused'} - ${trackName} by ${artistName}`);
                    this.dotNetReference.invokeMethodAsync('OnPlaybackStateChanged', isPlaying, trackName, artistName, imageUrl);
                }
            });
            
            // Ready
            this.player.addListener('ready', ({ device_id }) => {
                console.log('Player Ready with Device ID', device_id);
                this.deviceId = device_id;
                
                if (this.dotNetReference) {
                    console.log("Notifying .NET that player is ready");
                    this.dotNetReference.invokeMethodAsync('OnPlayerReady');
                }
            });
            
            // Not Ready
            this.player.addListener('not_ready', ({ device_id }) => {
                console.log('Device ID has gone offline', device_id);
                this.deviceId = null;
            });
            
            // Connect to the player
            console.log("Connecting to Spotify player...");
            this.player.connect()
                .then(success => {
                    if (success) {
                        console.log("Successfully connected to Spotify!");
                    } else {
                        console.error("Failed to connect to Spotify player");
                    }
                })
                .catch(error => {
                    console.error("Error connecting to Spotify player:", error);
                });
        } catch (error) {
            console.error("Exception creating Spotify player:", error);
            if (this.dotNetReference) {
                this.dotNetReference.invokeMethodAsync('OnPlayerError', error.toString());
            }
        }
    },
    
    // Play the current track
    play: async function() {
        if (!this.player || !this.deviceId) {
            console.error('Player not ready');
            return;
        }
        
        try {
            // First try to resume the current playback
            const response = await fetch(`https://api.spotify.com/v1/me/player/play?device_id=${this.deviceId}`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({})
            });
            
            if (response.status === 404) {
                // If no active context, start playing user's saved tracks
                const tracksResponse = await fetch('https://api.spotify.com/v1/me/tracks?limit=50', {
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
                
                if (tracksResponse.ok) {
                    const tracksData = await tracksResponse.json();
                    if (tracksData.items && tracksData.items.length > 0) {
                        // Extract track URIs
                        const trackUris = tracksData.items.map(item => item.track.uri);
                        
                        // Start playing saved tracks
                        await fetch(`https://api.spotify.com/v1/me/player/play?device_id=${this.deviceId}`, {
                            method: 'PUT',
                            headers: {
                                'Authorization': `Bearer ${this.token}`,
                                'Content-Type': 'application/json'
                            },
                            body: JSON.stringify({
                                uris: trackUris
                            })
                        });
                    }
                }
            }
        } catch (error) {
            console.error('Error playing track:', error);
        }
    },
    
    // Play a specific URI
    playUri: async function(spotifyUri) {
        if (!this.player || !this.deviceId) return false;
        
        try {
            const response = await fetch(`https://api.spotify.com/v1/me/player/play?device_id=${this.deviceId}`, {
                method: 'PUT',
                body: JSON.stringify({ 
                    uris: [spotifyUri] 
                }),
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.token}`
                },
            });
            
            return response.status >= 200 && response.status < 300;
        } catch (error) {
            console.error('Error playing track:', error);
            return false;
        }
    },
    
    // Pause playback
    pause: async function() {
        if (!this.player) return;
        
        try {
            await this.player.pause();
        } catch (error) {
            console.error('Error pausing track:', error);
        }
    },
    
    // Toggle play/pause
    togglePlay: async function() {
        if (!this.player) return false;
        await this.player.togglePlay();
        return true;
    },
    
    // Skip to previous track
    previousTrack: async function() {
        if (!this.player) return;
        
        try {
            await this.player.previousTrack();
        } catch (error) {
            console.error('Error going to previous track:', error);
        }
    },
    
    // Skip to next track
    nextTrack: async function() {
        if (!this.player) return;
        
        try {
            await this.player.nextTrack();
        } catch (error) {
            console.error('Error going to next track:', error);
        }
    },
    
    // Seek to position
    seek: async function(positionMs) {
        if (!this.player) return false;
        await this.player.seek(positionMs);
        return true;
    },
    
    // Set volume (0-1)
    setVolume: async function(volume) {
        if (!this.player) return;
        
        try {
            await this.player.setVolume(volume);
        } catch (error) {
            console.error('Error setting volume:', error);
        }
    },
    
    // Get current player state
    getState: async function() {
        if (!this.player) return null;
        return await this.player.getCurrentState();
    },
    
    // Disconnect the player
    disconnect: function() {
        if (this.player) {
            this.player.disconnect();
            this.player = null;
            this.deviceId = null;
        }
    }
};