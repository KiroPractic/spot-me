window.spotifyPlayer = {
    player: null,
    deviceId: null,
    
    initialize: function(token) {
        return new Promise((resolve, reject) => {
            window.onSpotifyWebPlaybackSDKReady = () => {
                this.player = new Spotify.Player({
                    name: 'SpotMe Player',
                    getOAuthToken: cb => { cb(token); },
                    volume: 0.5
                });

                // Error handling
                this.player.addListener('initialization_error', ({ message }) => {
                    console.error('Initialization error:', message);
                    reject('Initialization error: ' + message);
                });

                this.player.addListener('authentication_error', ({ message }) => {
                    console.error('Authentication error:', message);
                    reject('Authentication error: ' + message);
                });

                this.player.addListener('account_error', ({ message }) => {
                    console.error('Account error:', message);
                    reject('Account error: ' + message);
                });

                this.player.addListener('playback_error', ({ message }) => {
                    console.error('Playback error:', message);
                });

                // Ready
                this.player.addListener('ready', ({ device_id }) => {
                    console.log('Ready with Device ID', device_id);
                    this.deviceId = device_id;
                    resolve(device_id);
                });

                // Not Ready
                this.player.addListener('not_ready', ({ device_id }) => {
                    console.log('Device ID has gone offline', device_id);
                });

                // Connect to the player
                this.player.connect();
            };
        });
    },
    
    play: async function(spotifyUri) {
        if (!this.player || !this.deviceId) return false;
        
        try {
            const response = await fetch(`https://api.spotify.com/v1/me/player/play?device_id=${this.deviceId}`, {
                method: 'PUT',
                body: JSON.stringify({ 
                    uris: [spotifyUri] 
                }),
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${await this.player._options.getOAuthToken(t => t)}`
                },
            });
            
            return response.status >= 200 && response.status < 300;
        } catch (error) {
            console.error('Error playing track:', error);
            return false;
        }
    },
    
    togglePlay: async function() {
        if (!this.player) return false;
        await this.player.togglePlay();
        return true;
    },
    
    next: async function() {
        if (!this.player) return false;
        await this.player.nextTrack();
        return true;
    },
    
    previous: async function() {
        if (!this.player) return false;
        await this.player.previousTrack();
        return true;
    },
    
    seek: async function(positionMs) {
        if (!this.player) return false;
        await this.player.seek(positionMs);
        return true;
    },
    
    setVolume: async function(volumePercent) {
        if (!this.player) return false;
        await this.player.setVolume(volumePercent);
        return true;
    },
    
    getState: async function() {
        if (!this.player) return null;
        return await this.player.getCurrentState();
    },
    
    disconnect: function() {
        if (this.player) {
            this.player.disconnect();
            this.player = null;
            this.deviceId = null;
        }
    }
};