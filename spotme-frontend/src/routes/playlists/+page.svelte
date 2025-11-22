<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { spotifyApi, type Playlist } from '$lib/api/spotify';
	
	export let params: Record<string, string> = {};
	
	let playlists: Playlist[] = [];
	let authUrl = '';
	let loading = true;
	let error = '';
	
	onMount(() => {
		if (!$authStore.isAuthenticated) {
			goto('/login');
			return;
		}
		loadAuthUrl();
		loadPlaylists();
	});
	
	async function loadAuthUrl() {
		try {
			const response = await spotifyApi.getAuthUrl();
			authUrl = response.authUrl;
		} catch (err) {
			console.error('Failed to get auth URL:', err);
		}
	}
	
	async function loadPlaylists() {
		loading = true;
		error = '';
		
		try {
			const response = await spotifyApi.getPlaylists();
			playlists = response.playlists;
		} catch (err: any) {
			if (err.status === 401) {
				error = 'Please connect with Spotify to view your playlists';
			} else {
				error = 'Failed to load playlists';
			}
		} finally {
			loading = false;
		}
	}
</script>

<div class="container playlists-container mt-4">
	<h1>My Playlists</h1>
	
	{#if authUrl}
		<div id="spotify-auth-section" class="mb-4">
			<a href={authUrl} class="btn btn-primary spotify-button">
				<i class="bi bi-spotify me-2"></i>
				Connect with Spotify
			</a>
		</div>
	{/if}
	
	{#if loading}
		<div class="text-center">
			<div class="spinner-border" role="status">
				<span class="visually-hidden">Loading...</span>
			</div>
		</div>
	{:else if error}
		<div class="alert alert-warning">{error}</div>
	{:else if playlists.length > 0}
		<div id="playlists-content" class="playlists-grid">
			{#each playlists as playlist}
				<div class="playlist-card">
					<div class="playlist-image">
						{#if playlist.images && playlist.images.length > 0}
							<img src={playlist.images[0].url} alt={playlist.name} />
						{:else}
							<div class="playlist-image-placeholder">
								<i class="bi bi-music-note-list"></i>
							</div>
						{/if}
					</div>
					<div class="playlist-details">
						<h5 class="playlist-name">{playlist.name}</h5>
						{#if playlist.description}
							<p class="playlist-description">{playlist.description}</p>
						{/if}
						<div class="playlist-meta">
							<span class="playlist-tracks">{playlist.tracks?.total || 0} tracks</span>
							{#if playlist.collaborative}
								<span class="playlist-collaborative">Collaborative</span>
							{/if}
							{#if !playlist.public}
								<span class="playlist-private">Private</span>
							{/if}
						</div>
						{#if playlist.external_urls?.spotify}
							<a 
								href={playlist.external_urls.spotify} 
								target="_blank" 
								rel="noopener noreferrer"
								class="open-spotify-btn"
							>
								<i class="bi bi-box-arrow-up-right me-1"></i>
								Open in Spotify
							</a>
						{/if}
					</div>
				</div>
			{/each}
		</div>
	{:else}
		<div class="alert alert-info">No playlists found</div>
	{/if}
</div>

