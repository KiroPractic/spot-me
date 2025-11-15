<script lang="ts">
	import { page } from '$app/stores';
	import { authStore } from '$lib/stores/auth';
	import { goto } from '$app/navigation';
	
	function handleLogout() {
		authStore.clearToken();
		goto('/login');
	}
</script>

<nav class="navbar navbar-dark bg-dark">
	<div class="container">
		<a class="navbar-brand" href="/">
			<i class="bi bi-spotify"></i> SpotMe
		</a>
		{#if $authStore.isAuthenticated}
			<div>
				<a href="/data" class="text-light me-3" class:active={$page.url.pathname === '/data'}>
					My Data
				</a>
				<a href="/stats" class="text-light me-3" class:active={$page.url.pathname === '/stats'}>
					Stats
				</a>
				<a href="/playlists" class="text-light me-3" class:active={$page.url.pathname === '/playlists'}>
					Playlists
				</a>
				<button type="button" on:click={handleLogout} class="btn btn-link text-light p-0 border-0" style="text-decoration: none;">
					Logout
				</button>
			</div>
		{/if}
	</div>
</nav>

<style>
	.active {
		font-weight: 600;
	}
</style>

