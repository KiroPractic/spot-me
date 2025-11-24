<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';

	export let params: Record<string, string> = {};

	onMount(() => {
		if ($authStore.isAuthenticated) {
			goto('/data');
		}
	});
</script>

<div class="landing-container">
	<!-- Hero Section -->
	<section class="hero-section">
		<div class="container">
			<div class="row align-items-center">
				<div class="col-lg-6">
					<div class="hero-content">
						<h1 class="display-4 fw-bold text-primary mb-4">
							Unlock Your 
							<span class="text-gradient">Spotify Story</span>
						</h1>
						<p class="lead mb-4">
							Dive deep into your music listening patterns, discover hidden insights, 
							and explore your personal Spotify journey like never before.
						</p>
						<div class="hero-actions">
							<a href="/register" class="spotify-button me-3">
								<i class="bi bi-person-plus me-2"></i>
								Get Started Free
							</a>
							<a href="/login" class="spotify-button-secondary">
								<i class="bi bi-box-arrow-in-right me-2"></i>
								Sign In
							</a>
						</div>
					</div>
				</div>
				<div class="col-lg-6">
					<div class="hero-image">
						<div class="music-visualization">
							<!-- Left sound waves -->
							<div class="sound-waves sound-waves-left">
								<div class="sound-wave" style="--wave-index: 0;"></div>
								<div class="sound-wave" style="--wave-index: 1;"></div>
								<div class="sound-wave" style="--wave-index: 2;"></div>
								<div class="sound-wave" style="--wave-index: 3;"></div>
								<div class="sound-wave" style="--wave-index: 4;"></div>
								<div class="sound-wave" style="--wave-index: 5;"></div>
							</div>
							<!-- Right sound waves -->
							<div class="sound-waves sound-waves-right">
								<div class="sound-wave" style="--wave-index: 0;"></div>
								<div class="sound-wave" style="--wave-index: 1;"></div>
								<div class="sound-wave" style="--wave-index: 2;"></div>
								<div class="sound-wave" style="--wave-index: 3;"></div>
								<div class="sound-wave" style="--wave-index: 4;"></div>
								<div class="sound-wave" style="--wave-index: 5;"></div>
							</div>
							<div class="vinyl-record">
								<div class="record-inner">
									<i class="bi bi-spotify"></i>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>
	</section>

	<!-- Features Section -->
	<section class="features-section py-5">
		<div class="container">
			<div class="row mb-5">
				<div class="col-12 text-center">
					<h2 class="display-5 fw-bold mb-3">Discover Your Music Insights</h2>
					<p class="lead text-muted">Comprehensive analytics for your Spotify listening habits</p>
				</div>
			</div>
			<div class="row g-4">
				<div class="col-md-4">
					<div class="feature-card">
						<div class="feature-icon">
							<i class="bi bi-graph-up-arrow"></i>
						</div>
						<h3 class="h4 mb-3">Detailed Statistics</h3>
						<p class="text-muted">
							Track your total listening time, play counts, and discover your most played artists, 
							albums, and tracks. Filter by date ranges to see how your taste evolves over time.
						</p>
					</div>
				</div>
				<div class="col-md-4">
					<div class="feature-card">
						<div class="feature-icon">
							<i class="bi bi-music-note-beamed"></i>
						</div>
						<h3 class="h4 mb-3">Content Breakdown</h3>
						<p class="text-muted">
							Separate insights for music, podcasts, and audiobooks. See exactly how much time 
							you spend on each content type and track your listening diversity.
						</p>
					</div>
				</div>
				<div class="col-md-4">
					<div class="feature-card">
						<div class="feature-icon">
							<i class="bi bi-collection-play"></i>
						</div>
						<h3 class="h4 mb-3">Playlist Integration</h3>
						<p class="text-muted">
							Connect your Spotify account to browse and explore your playlists directly 
							from SpotATrend. Seamlessly integrated with your Spotify data.
						</p>
					</div>
				</div>
			</div>
		</div>
	</section>

	<!-- How It Works Section -->
	<section class="how-it-works-section py-5">
		<div class="container">
			<div class="row mb-5">
				<div class="col-12 text-center">
					<h2 class="display-5 fw-bold mb-3">How It Works</h2>
					<p class="lead text-muted">Get started in just a few simple steps</p>
				</div>
			</div>
			<div class="row g-4">
				<div class="col-md-3">
					<div class="step-card">
						<div class="step-number">1</div>
						<h4 class="h5 mb-3">Request Your Data</h4>
						<p class="text-muted">
							Visit Spotify's privacy settings and request your extended streaming history. 
							Spotify will first send a confirmation email - make sure to confirm your request. 
							Then you'll receive a download link within a few days.
						</p>
					</div>
				</div>
				<div class="col-md-3">
					<div class="step-card">
						<div class="step-number">2</div>
						<h4 class="h5 mb-3">Upload Your Files</h4>
						<p class="text-muted">
							Download and extract the ZIP file from Spotify, then upload your JSON streaming 
							history files to SpotATrend. You can upload multiple files at once.
						</p>
					</div>
				</div>
				<div class="col-md-3">
					<div class="step-card">
						<div class="step-number">3</div>
						<h4 class="h5 mb-3">Explore Your Stats</h4>
						<p class="text-muted">
							View comprehensive statistics about your listening habits, including top artists, 
							total playtime, and content breakdowns. Filter by date ranges for deeper insights.
						</p>
					</div>
				</div>
				<div class="col-md-3">
					<div class="step-card">
						<div class="step-number">4</div>
						<h4 class="h5 mb-3">Connect Spotify</h4>
						<p class="text-muted">
							Optionally connect your Spotify account to browse your playlists and get even 
							more insights into your music collection.
						</p>
					</div>
				</div>
			</div>
		</div>
	</section>

	<!-- Benefits Section -->
	<section class="benefits-section py-5">
		<div class="container">
			<div class="row align-items-center">
				<div class="col-lg-6">
					<h2 class="display-5 fw-bold mb-4">Why Use SpotATrend?</h2>
					<div class="benefit-item mb-4">
						<div class="d-flex align-items-start">
							<div class="benefit-icon me-3">
								<i class="bi bi-shield-check"></i>
							</div>
							<div>
								<h4 class="h5 mb-2">Privacy First</h4>
								<p class="text-muted mb-0">
									Your data stays private and secure. We process your streaming history locally 
									and never share your information with third parties.
								</p>
							</div>
						</div>
					</div>
					<div class="benefit-item mb-4">
						<div class="d-flex align-items-start">
							<div class="benefit-icon me-3">
								<i class="bi bi-calendar-range"></i>
							</div>
							<div>
								<h4 class="h5 mb-2">Historical Analysis</h4>
								<p class="text-muted mb-0">
									Analyze your listening patterns across any time period. See how your music 
									taste has evolved over months or years.
								</p>
							</div>
						</div>
					</div>
					<div class="benefit-item mb-4">
						<div class="d-flex align-items-start">
							<div class="benefit-icon me-3">
								<i class="bi bi-speedometer2"></i>
							</div>
							<div>
								<h4 class="h5 mb-2">Fast & Reliable</h4>
								<p class="text-muted mb-0">
									Built with modern technology for fast processing and reliable performance. 
									Your data is processed efficiently and accurately.
								</p>
							</div>
						</div>
					</div>
					<div class="benefit-item">
						<div class="d-flex align-items-start">
							<div class="benefit-icon me-3">
								<i class="bi bi-gift"></i>
							</div>
							<div>
								<h4 class="h5 mb-2">Completely Free</h4>
								<p class="text-muted mb-0">
									SpotATrend is free to use. No subscriptions, no hidden fees. Just upload your 
									data and start exploring your music journey.
								</p>
							</div>
						</div>
					</div>
				</div>
				<div class="col-lg-6 text-center">
					<div class="benefits-visual">
						<i class="bi bi-bar-chart-fill"></i>
					</div>
				</div>
			</div>
		</div>
	</section>

	<!-- CTA Section -->
	<section class="cta-section py-5">
		<div class="container">
			<div class="row">
				<div class="col-12 text-center">
					<h2 class="display-5 fw-bold mb-3">Ready to Explore Your Music Story?</h2>
					<p class="lead text-muted mb-4">
						Join SpotATrend today and discover insights about your Spotify listening habits
					</p>
					<div class="cta-actions">
						<a href="/register" class="spotify-button me-3">
							<i class="bi bi-person-plus me-2"></i>
							Get Started Free
						</a>
						<a href="/login" class="spotify-button-secondary">
							<i class="bi bi-box-arrow-in-right me-2"></i>
							Sign In
						</a>
					</div>
				</div>
			</div>
		</div>
	</section>
</div>

