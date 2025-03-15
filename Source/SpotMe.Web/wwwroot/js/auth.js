function scheduleLogout() {
    const token = localStorage.getItem("spotify_access_token");
    const expiryStr = localStorage.getItem("spotify_token_expiry");
    // Only take action if both token and expiry exist
    if (token || expiryStr) {
        try {
            // Check if we're already on the logout page to prevent infinite redirects
            const currentPath = window.location.pathname.toLowerCase();
            const isOnLogoutPage = currentPath === '/logout';
     
            // Only proceed if we're not already on logout pages
            if (!isOnLogoutPage) {
                const expiry = new Date(expiryStr);
                const timeUntilExpiry = expiry.getTime() - Date.now();
                console.log(`Token expires in ${Math.floor(timeUntilExpiry / 1000)} seconds`);

                if (timeUntilExpiry <= 0) {
                    // Token exists but has expired, redirect to logout
                    console.log('Token has expired, redirecting to logout');
                    window.location.href = '/logout';
                }
            }
        } catch (error) {
            console.error('Error handling token expiry:', error);
        }
    } else {
        // No token or expiry in localStorage, do nothing
        console.log('No token or expiry found, no need to schedule logout');
    }
}
window.addEventListener('load', scheduleLogout);

function logoutUser() {
    localStorage.removeItem("spotify_access_token");
    localStorage.removeItem("spotify_token_expiry");
    window.location.replace("/auth");
}