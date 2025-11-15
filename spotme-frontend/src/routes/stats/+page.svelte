<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { statsApi, type Stats } from '$lib/api/stats';
	
	let startDate = '';
	let endDate = '';
	let stats: Stats | null = null;
	let loading = true;
	let error = '';
	
	onMount(() => {
		if (!$authStore.isAuthenticated) {
			goto('/login');
			return;
		}
		loadStats();
	});
	
	async function loadStats() {
		loading = true;
		error = '';
		
		try {
			const response = await statsApi.getStats(
				startDate || undefined,
				endDate || undefined
			);
			stats = response.stats;
		} catch (err) {
			error = 'Failed to load stats';
			console.error(err);
		} finally {
			loading = false;
		}
	}
	
	function formatTime(minutes: number): string {
		const hours = Math.floor(minutes / 60);
		const mins = Math.floor(minutes % 60);
		return `${hours}h ${mins}m`;
	}
</script>

<div class="container stats-container mt-4">
	<h1>Stats</h1>
	<div class="date-range-picker mb-4">
		<form id="stats-form" on:submit|preventDefault={loadStats}>
			<div class="row g-3 align-items-end">
				<div class="col-auto">
					<label for="startDate" class="form-label">Start Date</label>
					<input 
						type="date" 
						class="form-control dark-input" 
						id="startDate" 
						bind:value={startDate}
					/>
				</div>
				<div class="col-auto">
					<label for="endDate" class="form-label">End Date</label>
					<input 
						type="date" 
						class="form-control dark-input" 
						id="endDate" 
						bind:value={endDate}
					/>
				</div>
				<div class="col-auto">
					<button type="submit" class="btn btn-primary">Apply</button>
				</div>
			</div>
		</form>
	</div>
	
	{#if loading}
		<div class="text-center">
			<div class="spinner-border" role="status">
				<span class="visually-hidden">Loading...</span>
			</div>
		</div>
	{:else if error}
		<div class="alert alert-danger">{error}</div>
	{:else if stats}
		<div id="stats-content">
			<div class="row g-4 mb-4">
				<div class="col-md-6">
					<div class="card stat-card">
						<div class="card-body">
							<h5 class="card-title">Total Playtime</h5>
							<p class="card-text display-6">{formatTime(stats.totalMinutes)}</p>
						</div>
					</div>
				</div>
				<div class="col-md-6">
					<div class="card stat-card">
						<div class="card-body">
							<h5 class="card-title">Total Plays</h5>
							<p class="card-text display-6">{stats.totalTracks.toLocaleString()}</p>
						</div>
					</div>
				</div>
			</div>
			<div class="row g-4 mb-4">
				<div class="col-md-4">
					<div class="card stat-card">
						<div class="card-body text-center">
							<h6 class="card-title">Music</h6>
							<p class="card-text h4">{formatTime(stats.contentTypeBreakdown.audioTrackMinutes)}</p>
							<small class="text-muted">{stats.contentTypeBreakdown.audioTrackCount} tracks</small>
						</div>
					</div>
				</div>
				<div class="col-md-4">
					<div class="card stat-card">
						<div class="card-body text-center">
							<h6 class="card-title">Podcasts</h6>
							<p class="card-text h4">{formatTime(stats.contentTypeBreakdown.podcastMinutes)}</p>
							<small class="text-muted">{stats.contentTypeBreakdown.podcastCount} episodes</small>
						</div>
					</div>
				</div>
				<div class="col-md-4">
					<div class="card stat-card">
						<div class="card-body text-center">
							<h6 class="card-title">Unique Artists</h6>
							<p class="card-text h4">{stats.uniqueArtists.toLocaleString()}</p>
						</div>
					</div>
				</div>
			</div>
		</div>
	{:else}
		<div class="alert alert-warning">No stats data available</div>
	{/if}
</div>

