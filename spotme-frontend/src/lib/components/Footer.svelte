<footer class="site-footer">
	<div class="container">
		<div class="row g-4">
			<div class="col-md-4">
				<h5 class="footer-brand">
					<i class="bi bi-spotify"></i> SpotMe
				</h5>
				<p class="footer-description">
					Unlock insights into your Spotify listening habits. Discover your music story with detailed analytics and statistics.
				</p>
			</div>
			<div class="col-md-2">
				<h6 class="footer-heading">Product</h6>
				<ul class="footer-links">
					<li><a href="/">Home</a></li>
					<li><a href="/register">Get Started</a></li>
					<li><a href="/login">Sign In</a></li>
				</ul>
			</div>
			<div class="col-md-2">
				<h6 class="footer-heading">Features</h6>
				<ul class="footer-links">
					<li><a href="/stats">Statistics</a></li>
					<li><a href="/data">Data Upload</a></li>
					<li><a href="/playlists">Playlists</a></li>
				</ul>
			</div>
			<div class="col-md-4">
				<h6 class="footer-heading">About</h6>
				<p class="footer-text">
					SpotMe helps you analyze your Spotify streaming history to discover insights about your music listening patterns, favorite artists, and listening habits.
				</p>
			</div>
		</div>
		<hr class="footer-divider">
		<div class="row">
			<div class="col-12">
				<div class="footer-bottom">
					<p class="footer-copyright">
						&copy; {new Date().getFullYear()} SpotMe. All rights reserved.
					</p>
					<div class="footer-social">
						<a href="https://open.spotify.com" target="_blank" rel="noopener noreferrer" class="footer-social-link" title="Spotify">
							<i class="bi bi-spotify"></i>
						</a>
					</div>
				</div>
			</div>
		</div>
	</div>
</footer>

<style>
	.site-footer {
		background-color: var(--dark-card);
		border-top: 1px solid var(--dark-border);
		padding: 3rem 0 1.5rem;
		margin-top: auto;
	}

	.footer-brand {
		color: var(--text-primary);
		font-size: 1.5rem;
		font-weight: 700;
		margin-bottom: 1rem;
	}

	.footer-brand i {
		color: var(--spotify-green);
	}

	.footer-description {
		color: var(--text-secondary);
		font-size: 0.9rem;
		line-height: 1.6;
		margin-bottom: 0;
	}

	.footer-heading {
		color: var(--text-primary);
		font-size: 1rem;
		font-weight: 600;
		margin-bottom: 1rem;
	}

	.footer-links {
		list-style: none;
		padding: 0;
		margin: 0;
	}

	.footer-links li {
		margin-bottom: 0.5rem;
	}

	.footer-links a {
		color: var(--text-secondary);
		text-decoration: none;
		font-size: 0.9rem;
		transition: color 0.2s ease;
	}

	.footer-links a:hover {
		color: var(--spotify-green);
		text-decoration: none;
	}

	.footer-text {
		color: var(--text-secondary);
		font-size: 0.9rem;
		line-height: 1.6;
		margin-bottom: 0;
	}

	.footer-divider {
		border-color: var(--dark-border);
		margin: 2rem 0 1.5rem;
		opacity: 0.5;
	}

	.footer-bottom {
		display: flex;
		justify-content: space-between;
		align-items: center;
		flex-wrap: wrap;
		gap: 1rem;
	}

	.footer-copyright {
		color: var(--text-muted);
		font-size: 0.85rem;
		margin: 0;
	}

	.footer-social {
		display: flex;
		gap: 1rem;
	}

	.footer-social-link {
		color: var(--text-secondary);
		font-size: 1.25rem;
		text-decoration: none;
		transition: color 0.2s ease, transform 0.2s ease;
	}

	.footer-social-link:hover {
		color: var(--spotify-green);
		transform: translateY(-2px);
		text-decoration: none;
	}

	@media (max-width: 768px) {
		.site-footer {
			padding: 2rem 0 1rem;
		}

		.footer-bottom {
			flex-direction: column;
			text-align: center;
		}
	}
</style>

