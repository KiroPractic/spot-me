// Navbar visibility fix
(function() {
    function checkNavbarVisibility() {
        console.log("Checking navbar visibility...");
        var hasToken = !!localStorage.getItem('spotify_access_token');
        console.log("Token exists: " + hasToken);

        if (hasToken) {
            // Force-create the navbar if it doesn't exist
            var navbar = document.getElementById('spotme-navbar');
            
            if (!navbar) {
                console.log("Navbar not found, creating it manually");
                
                // Create our own navbar
                navbar = document.createElement('div');
                navbar.id = 'spotme-navbar';
                navbar.setAttribute('style', 'position: fixed; top: 0; left: 0; right: 0; height: 60px; background-color: #121212; color: #fff; display: flex; align-items: center; justify-content: space-between; padding: 0 20px; z-index: 1000; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);');
                
                // Create navbar content
                var brand = document.createElement('div');
                brand.setAttribute('style', 'display: flex; align-items: center; font-size: 1.5rem; font-weight: 700;');
                brand.innerHTML = '<i class="bi bi-spotify" style="color: #1DB954; font-size: 1.75rem; margin-right: 10px;"></i><span>SpotMe</span>';
                
                var navLinks = document.createElement('div');
                navLinks.setAttribute('style', 'display: flex; list-style: none; margin: 0; padding: 0;');
                navLinks.innerHTML = `
                    <a href="/" style="display: flex; align-items: center; padding: 8px 12px; color: #fff; text-decoration: none; border-radius: 4px; margin: 0 5px;">
                        <i class="bi bi-house-door" style="margin-right: 8px;"></i>
                        <span>Home</span>
                    </a>
                    <a href="/search" style="display: flex; align-items: center; padding: 8px 12px; color: #b3b3b3; text-decoration: none; border-radius: 4px; margin: 0 5px;">
                        <i class="bi bi-search" style="margin-right: 8px;"></i>
                        <span>Search</span>
                    </a>
                    <a href="/playlists" style="display: flex; align-items: center; padding: 8px 12px; color: #b3b3b3; text-decoration: none; border-radius: 4px; margin: 0 5px;">
                        <i class="bi bi-music-note-list" style="margin-right: 8px;"></i>
                        <span>Playlists</span>
                    </a>
                `;
                
                var userSection = document.createElement('div');
                userSection.setAttribute('style', 'display: flex; align-items: center;');
                
                // Get username from localStorage or use default
                var username = localStorage.getItem('spotify_user_name') || 'Spotify User';
                
                var userProfile = document.createElement('div');
                userProfile.setAttribute('style', 'display: flex; align-items: center; margin-right: 15px;');
                userProfile.innerHTML = `
                    <i class="bi bi-person-circle" style="font-size: 24px; color: #b3b3b3; margin-right: 10px;"></i>
                    <span style="color: #fff; font-weight: 500;">${username}</span>
                `;
                
                var logoutBtn = document.createElement('button');
                logoutBtn.setAttribute('style', 'background-color: transparent; border: 1px solid #333; color: #b3b3b3; display: flex; align-items: center; padding: 6px 12px; border-radius: 4px; cursor: pointer;');
                logoutBtn.innerHTML = '<i class="bi bi-box-arrow-right" style="margin-right: 6px;"></i><span>Logout</span>';
                logoutBtn.onclick = function() {
                    // Clear token and reload
                    localStorage.removeItem('spotify_access_token');
                    localStorage.removeItem('spotify_token_expiry');
                    localStorage.removeItem('spotify_user_name');
                    window.location.href = '/auth';
                };
                
                userSection.appendChild(userProfile);
                userSection.appendChild(logoutBtn);
                
                // Add all elements to navbar
                navbar.appendChild(brand);
                navbar.appendChild(navLinks);
                navbar.appendChild(userSection);
                
                // Add to DOM
                document.body.insertBefore(navbar, document.body.firstChild);
                
                // Update main content padding
                var mainContent = document.querySelector('.main-content');
                if (mainContent) {
                    mainContent.classList.add('with-navbar');
                }
            } else {
                console.log("Navbar found, ensuring it's visible");
                navbar.style.display = 'flex';
            }
            
            // Update debug panel
            var debugPanel = document.getElementById('auth-debug-panel');
            if (debugPanel) {
                debugPanel.innerHTML = 'Auth: true, Profile: ' + (localStorage.getItem('spotify_user_name') || 'JS Fix');
            }
        }
    }

    // Run immediately and with delays to catch different rendering situations
    setTimeout(checkNavbarVisibility, 100);
    setTimeout(checkNavbarVisibility, 500);
    setTimeout(checkNavbarVisibility, 1000);
    
    // Also run on page load complete
    window.addEventListener('load', checkNavbarVisibility);

    // Expose to window for debugging
    window.spotmeFix = {
        checkNavbar: checkNavbarVisibility
    };
})();