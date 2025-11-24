<script lang="ts">
	import { page } from '$app/stores';
	import { authStore } from '$lib/stores/auth';
	import { goto } from '$app/navigation';
	
	function handleLogout() {
		authStore.clearToken();
		goto('/login');
	}
</script>

<nav class="navbar navbar-dark navbar-transparent">
	<div class="container navbar-container">
		<a class="navbar-brand" href="/">
			<i class="bi bi-spotify"></i> SpotATrend
		</a>
		{#if $authStore.isAuthenticated}
			<div class="navbar-nav">
				<a href="/data" class="text-light nav-item" class:active={$page.url.pathname === '/data'}>
					My Data
				</a>
				<a href="/stats" class="text-light nav-item" class:active={$page.url.pathname === '/stats'}>
					Stats
				</a>
				<a href="/playlists" class="text-light nav-item" class:active={$page.url.pathname === '/playlists'}>
					Playlists
				</a>
			</div>
			<button type="button" on:click={handleLogout} class="text-light nav-item nav-button logout-button">
				Logout
			</button>
		{/if}
	</div>
</nav>

<style>
	.navbar {
		display: flex !important;
		flex-direction: row !important;
	}
	
	.navbar-transparent {
		background-color: transparent !important;
		backdrop-filter: blur(10px);
		-webkit-backdrop-filter: blur(10px);
	}
	
	.navbar-container {
		display: grid !important;
		grid-template-columns: auto 1fr auto;
		align-items: center;
		width: 100%;
		gap: 1rem;
	}
	
	.navbar-nav {
		display: flex !important;
		flex-direction: row !important;
		align-items: center;
		justify-content: center;
		gap: 1.5rem;
	}
	
	.logout-button {
		justify-self: end;
	}
	
	.nav-item {
		text-decoration: none;
		padding: 0;
		background: none;
		border: none;
		cursor: pointer;
		font-size: inherit;
		font-family: inherit;
		transition: color 0.2s ease;
		white-space: nowrap;
	}
	
	.nav-item:hover {
		color: var(--spotify-green) !important;
		text-decoration: none;
	}
	
	.nav-button {
		text-decoration: none;
		padding: 0;
		background: none;
		border: none;
		cursor: pointer;
		font-size: inherit;
		font-family: inherit;
		transition: color 0.2s ease;
		white-space: nowrap;
	}
	
	.nav-button:hover {
		color: var(--spotify-green) !important;
		text-decoration: none;
	}
	
	.active {
		font-weight: 600;
	}
</style>

