<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { statsApi, type Stats } from '$lib/api/stats';
	import DayOfWeekChart from '$lib/components/DayOfWeekChart.svelte';
	import HourOfDayChart from '$lib/components/HourOfDayChart.svelte';
	import MonthlyChart from '$lib/components/MonthlyChart.svelte';
	import WorldMap from '$lib/components/WorldMap.svelte';
	import PlaybackBehaviorPieChart from '$lib/components/PlaybackBehaviorPieChart.svelte';
	
	export let params: Record<string, string> = {};
	
	let startDate = '';
	let endDate = '';
	let stats: Stats | null = null;
	let loading = true;
	let error = '';
	let showDays = false;
	
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
		if (showDays) {
			const days = Math.floor(minutes / (24 * 60));
			const hours = Math.floor((minutes % (24 * 60)) / 60);
			const mins = Math.floor(minutes % 60);
			
			if (days > 0) {
				return `${days}d ${hours}h ${mins}m`;
			} else {
				return `${hours}h ${mins}m`;
			}
		} else {
			const hours = Math.floor(minutes / 60);
			const mins = Math.floor(minutes % 60);
			return `${hours}h ${mins}m`;
		}
	}

	// Helper function to convert HSL to hex
	function hslToHex(h: number, s: number, l: number): string {
		l /= 100;
		const a = (s * Math.min(l, 1 - l)) / 100;
		const f = (n: number) => {
			const k = (n + h / 30) % 12;
			const color = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
			return Math.round(255 * color)
				.toString(16)
				.padStart(2, '0');
		};
		return `#${f(0)}${f(8)}${f(4)}`;
	}

	// Get full country name from country code (matching WorldMap component)
	function getCountryName(countryCode: string): string {
		try {
			const countryNameFormatter = new Intl.DisplayNames(['en'], { type: 'region' });
			return countryNameFormatter.of(countryCode.toUpperCase()) || countryCode.toUpperCase();
		} catch {
			return countryCode.toUpperCase();
		}
	}

	// Calculate color for country based on listening time (matching map color scale)
	// Uses logarithmic scale (blended with linear) to better distribute colors when most time is in one country
	function getCountryColor(minutes: number): string {
		if (!minutes || minutes === 0 || !stats?.countryStats || stats.countryStats.length === 0) {
			return '#1a1a1a';
		}
		
		// Find max value from country stats
		const maxMinutes = Math.max(...stats.countryStats.map(c => c.totalMinutes));
		
		if (maxMinutes === 0) return '#1a1a1a';
		
		// Use logarithmic scale: log(minutes + 1) / log(maxMinutes + 1)
		// Adding 1 to avoid log(0) and ensure smooth scaling
		const logValue = Math.log(minutes + 1);
		const logMax = Math.log(maxMinutes + 1);
		const logPercentage = (logValue / logMax) * 100;
		
		// Blend with linear scale (40% log, 60% linear) to make it less aggressive
		const linearPercentage = (minutes / maxMinutes) * 100;
		const percentage = logPercentage * 0.4 + linearPercentage * 0.6;
		
		// Base turquoise/cyan hue (~185°)
		const hue = 185;
		// Keep saturation constant at 100%
		const saturation = 100;
		// Scale lightness from 20% (darkest) to 82% (brightest) based on blended percentage
		// Top country (100%) gets 82% lightness, others scale down
		const lightness = 20 + (percentage / 100) * 62; // 20% to 82% range
		
		return hslToHex(hue, saturation, lightness);
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
		<div class="row mt-3">
			<div class="col-12">
				<label class="form-label mb-2">Display time stats in days and hours</label>
				<div class="form-check">
					<input 
						class="form-check-input" 
						type="radio" 
						name="timeFormat"
						id="hoursMinutes"
						checked={!showDays}
						on:change={() => showDays = false}
					/>
					<label class="form-check-label" for="hoursMinutes">
						Hours & Minutes
					</label>
				</div>
				<div class="form-check">
					<input 
						class="form-check-input" 
						type="radio" 
						name="timeFormat"
						id="daysHoursMinutes"
						checked={showDays}
						on:change={() => showDays = true}
					/>
					<label class="form-check-label" for="daysHoursMinutes">
						Days, Hours & Minutes
					</label>
				</div>
			</div>
		</div>
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
		{#key showDays}
		<div id="stats-content">
			<div class="row g-4 mb-4">
				<div class="col-md-6">
					<div class="card stat-card">
						<div class="card-body text-center">
							<h5 class="card-title">Total Playtime</h5>
							<p class="card-text display-6">{formatTime(stats.totalMinutes)}</p>
						</div>
					</div>
				</div>
				<div class="col-md-6">
					<div class="card stat-card">
						<div class="card-body text-center">
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
							<h6 class="card-title">Audiobooks</h6>
							<p class="card-text h4">{formatTime(stats.contentTypeBreakdown.audiobookMinutes)}</p>
							<small class="text-muted">{stats.contentTypeBreakdown.audiobookCount} chapters</small>
						</div>
					</div>
				</div>
			</div>
			
			{#if stats.timeBasedStats && stats.timeBasedStats.dayOfWeekStats && stats.timeBasedStats.dayOfWeekStats.length > 0}
				<h2 class="mb-4">Listening Patterns</h2>
				<DayOfWeekChart data={stats.timeBasedStats.dayOfWeekStats} />
			{/if}
			
			{#if stats.timeBasedStats && stats.timeBasedStats.hourOfDayStats && stats.timeBasedStats.hourOfDayStats.length > 0}
				<HourOfDayChart data={stats.timeBasedStats.hourOfDayStats} />
			{/if}
			
			{#if stats.timeBasedStats && stats.timeBasedStats.monthlyStats && stats.timeBasedStats.monthlyStats.length > 0}
				<MonthlyChart data={stats.timeBasedStats.monthlyStats} />
			{/if}
			
			{#if stats.countryStats && stats.countryStats.length > 0}
				<WorldMap data={stats.countryStats} />
				
				<div class="table-responsive mt-3">
					<table class="table table-hover table-dark">
						<thead>
							<tr>
								<th scope="col" class="text-center">#</th>
								<th scope="col">Country</th>
								<th scope="col" class="text-center">Listening Time</th>
								<th scope="col" class="text-center">Plays</th>
								<th scope="col" class="text-center">Artists</th>
								<th scope="col" class="text-center">Tracks</th>
							</tr>
						</thead>
						<tbody>
							{#each stats.countryStats as country, index}
								<tr>
									<td class="text-muted text-center">{index + 1}</td>
									<td>
										<span 
											class="country-color-indicator" 
											style="background-color: {getCountryColor(country.totalMinutes)}"
										></span>
										<strong>{getCountryName(country.countryCode)}</strong>
									</td>
									<td class="text-center">{formatTime(country.totalMinutes)}</td>
									<td class="text-center">{country.playCount.toLocaleString()}</td>
									<td class="text-center">{country.uniqueArtists.toLocaleString()}</td>
									<td class="text-center">{country.uniqueTracks.toLocaleString()}</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			{/if}
			
			{#if stats.musicStats}
				<h2 class="mb-4 mt-5">Music</h2>
				<div class="row g-4 mb-4">
					<div class="col-md-6">
						<div class="card stat-card">
							<div class="card-body text-center">
								<h5 class="card-title">Total Listening Time</h5>
								<p class="card-text display-6">{formatTime(stats.musicStats.totalMusicMinutes)}</p>
							</div>
						</div>
					</div>
					<div class="col-md-6">
						<div class="card stat-card">
							<div class="card-body text-center">
								<h5 class="card-title">Total Tracks</h5>
								<p class="card-text display-6">{stats.musicStats.totalMusicTracks.toLocaleString()}</p>
							</div>
						</div>
					</div>
				</div>
				<div class="row g-4 mb-4">
					<div class="col-md-4">
						<div class="card stat-card">
							<div class="card-body text-center">
								<h6 class="card-title">Unique Albums</h6>
								<p class="card-text h4">{stats.musicStats.uniqueMusicAlbums.toLocaleString()}</p>
							</div>
						</div>
					</div>
					<div class="col-md-4">
						<div class="card stat-card">
							<div class="card-body text-center">
								<h6 class="card-title">Unique Artists</h6>
								<p class="card-text h4">{stats.musicStats.uniqueMusicArtists.toLocaleString()}</p>
							</div>
						</div>
					</div>
					<div class="col-md-4">
						<div class="card stat-card">
							<div class="card-body text-center">
								<h6 class="card-title">Unique Tracks</h6>
								<p class="card-text h4">{stats.musicStats.uniqueMusicTracks.toLocaleString()}</p>
							</div>
						</div>
					</div>
				</div>
				
				{#if stats.musicStats.musicPlaybackBehavior}
					<PlaybackBehaviorPieChart data={stats.musicStats.musicPlaybackBehavior} />
				{/if}
				
				{#if stats.musicStats.topMusicArtists && stats.musicStats.topMusicArtists.length > 0}
					<h3 class="mb-3">Most Listened Artists</h3>
					<div class="table-responsive">
						<table class="table table-hover table-dark">
							<thead>
								<tr>
									<th scope="col" class="text-center">#</th>
									<th scope="col">Artist</th>
									<th scope="col" class="text-center">Listening Time</th>
									<th scope="col" class="text-center">Plays</th>
									<th scope="col" class="text-center">Tracks</th>
									<th scope="col" class="text-center">Albums</th>
								</tr>
							</thead>
							<tbody>
								{#each stats.musicStats.topMusicArtists as artist, index}
									<tr>
										<td class="text-muted text-center">{index + 1}</td>
										<td><strong>{artist.artistName}</strong></td>
										<td class="text-center">{formatTime(artist.totalMinutes)}</td>
										<td class="text-center">{artist.playCount.toLocaleString()}</td>
										<td class="text-center">{artist.uniqueTracks.toLocaleString()}</td>
										<td class="text-center">{artist.uniqueAlbums.toLocaleString()}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
				
				{#if stats.musicStats.topMusicTracks && stats.musicStats.topMusicTracks.length > 0}
					<h3 class="mb-3 mt-5">Most Listened Songs</h3>
					<div class="table-responsive">
						<table class="table table-hover table-dark">
							<thead>
								<tr>
									<th scope="col" class="text-center">#</th>
									<th scope="col">Song</th>
									<th scope="col">Artist</th>
									<th scope="col" class="text-center">Listening Time</th>
									<th scope="col" class="text-center">Plays</th>
								</tr>
							</thead>
							<tbody>
								{#each stats.musicStats.topMusicTracks as track, index}
									<tr>
										<td class="text-muted text-center">{index + 1}</td>
										<td><strong>{track.trackName}</strong></td>
										<td>{track.artistName}</td>
										<td class="text-center">{formatTime(track.totalMinutes)}</td>
										<td class="text-center">{track.playCount.toLocaleString()}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
				
				{#if stats.musicStats.topMusicAlbums && stats.musicStats.topMusicAlbums.length > 0}
					<h3 class="mb-3 mt-5">Most Listened Albums</h3>
					<div class="table-responsive">
						<table class="table table-hover table-dark">
							<thead>
								<tr>
									<th scope="col" class="text-center">#</th>
									<th scope="col">Album</th>
									<th scope="col">Artist</th>
									<th scope="col" class="text-center">Listening Time</th>
									<th scope="col" class="text-center">Plays</th>
									<th scope="col" class="text-center">Tracks</th>
								</tr>
							</thead>
							<tbody>
								{#each stats.musicStats.topMusicAlbums as album, index}
									<tr>
										<td class="text-muted text-center">{index + 1}</td>
										<td><strong>{album.albumName}</strong></td>
										<td>{album.artistName}</td>
										<td class="text-center">{formatTime(album.totalMinutes)}</td>
										<td class="text-center">{album.playCount.toLocaleString()}</td>
										<td class="text-center">{album.uniqueTracks.toLocaleString()}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			{/if}
		</div>
		{/key}
	{:else}
		<div class="alert alert-warning">No stats data available</div>
	{/if}
</div>

<style>
	.country-color-indicator {
		display: inline-block;
		width: 12px;
		height: 12px;
		border-radius: 50%;
		margin-right: 8px;
		vertical-align: middle;
		border: 1px solid rgba(255, 255, 255, 0.2);
	}
</style>

