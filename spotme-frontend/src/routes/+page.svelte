<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';

	onMount(() => {
		if ($authStore.isAuthenticated) {
			goto('/data');
		}
	});
</script>

<div class="landing-container">
	<section class="hero-section">
		<div class="container">
			<div class="row align-items-center min-vh-100">
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
</div>

