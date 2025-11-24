<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { browser } from '$app/environment';
	import type { CountryStats } from '$lib/api/stats';

	export let data: CountryStats[] = [];

	let mapContainer: HTMLDivElement;
	let mapInstance: any = null;

	onMount(async () => {
		if (!browser || !mapContainer || data.length === 0) return;

		// Dynamically import the library only on the client side
		const jsVectorMap = (await import('jsvectormap')).default;
		await import('jsvectormap/dist/maps/world.js');

		// Create a map of country code to total minutes
		const countryData: Record<string, number> = {};
		let maxMinutes = 0;

		data.forEach((stat) => {
			// Convert country code to lowercase for jsvectormap (it uses ISO 3166-1 alpha-2 codes)
			const code = stat.countryCode.toLowerCase();
			countryData[code] = stat.totalMinutes;
			if (stat.totalMinutes > maxMinutes) {
				maxMinutes = stat.totalMinutes;
			}
		});

		// Create a lookup map for country stats
		const countryStatsMap: Record<string, CountryStats> = {};
		data.forEach((stat) => {
			countryStatsMap[stat.countryCode.toLowerCase()] = stat;
		});

		// Helper function to convert HSL to hex
		const hslToHex = (h: number, s: number, l: number): string => {
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
		};
		
		// Calculate color based on logarithmic scale (blended with linear for less aggressive scaling)
		// Uses log scale to better distribute colors when most time is in one country
		const getColorForValue = (value: number, maxValue: number): string => {
			if (value === 0 || maxValue === 0) return '#1a1a1a';
			
			// Use logarithmic scale: log(value + 1) / log(maxValue + 1)
			// Adding 1 to avoid log(0) and ensure smooth scaling
			const logValue = Math.log(value + 1);
			const logMax = Math.log(maxValue + 1);
			const logPercentage = (logValue / logMax) * 100;
			
			// Blend with linear scale (40% log, 60% linear) to make it less aggressive
			const linearPercentage = (value / maxValue) * 100;
			const percentage = logPercentage * 0.4 + linearPercentage * 0.6;
			
			// Base turquoise/cyan hue (~185°)
			const hue = 185;
			// Keep saturation constant at 100%
			const saturation = 100;
			// Scale lightness from 20% (darkest) to 82% (brightest) based on blended percentage
			// Top country (100%) gets 82% lightness, others scale down
			const lightness = 20 + (percentage / 100) * 62; // 20% to 82% range
			
			return hslToHex(hue, saturation, lightness);
		};
		
		// Create set of countries with data for quick lookup
		const countriesWithData = new Set(Object.keys(countryData).map(c => c.toLowerCase()));
		
		// Initialize the map
		mapInstance = new jsVectorMap({
			selector: mapContainer,
			map: 'world',
			backgroundColor: '#0d1117', // Dark background
			regionStyle: {
				initial: {
					fill: '#1a1a1a', // Default dark color for countries with no data
					stroke: '#222',
					strokeWidth: 0.3,
					fillOpacity: 1,
					cursor: 'default'
				},
				hover: {
					fillOpacity: 0.8,
					cursor: 'pointer',
					stroke: '#555',
					strokeWidth: 0.5
				}
			},
			showTooltip: true,
			onRegionTooltipShow: function(event: Event, tooltip: any, code: string) {
				const countryStat = countryStatsMap[code.toLowerCase()];
				
				if (countryStat) {
					// Get full country name using Intl.DisplayNames
					const countryNameFormatter = new Intl.DisplayNames(['en'], { type: 'region' });
					const fullCountryName = countryNameFormatter.of(code.toUpperCase()) || code.toUpperCase();
					
					const hours = Math.floor(countryStat.totalMinutes / 60);
					const minutes = Math.floor(countryStat.totalMinutes % 60);
					
					// Style the tooltip to match the page design
					tooltip.css({
						'background-color': '#181818',
						'border': '1px solid #404040',
						'border-radius': '8px',
						'padding': '12px 16px',
						'color': '#ffffff',
						'font-family': "'Inter', 'Helvetica Neue', Helvetica, Arial, sans-serif",
						'font-size': '0.9rem',
						'box-shadow': '0 4px 12px rgba(0, 0, 0, 0.4)',
						'line-height': '1.6',
						'max-width': '250px'
					});
					
					tooltip.text(`
						<div style="font-weight: 700; font-size: 1rem; color: #ffffff; margin-bottom: 8px;">
							${fullCountryName}
						</div>
						<div style="color: #b3b3b3; font-size: 0.85rem;">
							<div style="margin-bottom: 4px;">${hours}h ${minutes}m total</div>
							<div style="margin-bottom: 4px;">${countryStat.playCount.toLocaleString()} plays</div>
							<div>${countryStat.uniqueArtists} artists</div>
						</div>
					`, true);
				} else {
					// Hide tooltip for countries without data
					event.preventDefault();
					return;
				}
			},
			onLoaded: function() {
				// Apply colors and disable hover for countries without data
				const applyColorsAndDisableHover = () => {
					if (!mapContainer) return;
					
					// Get all SVG paths
					const allPaths = mapContainer.querySelectorAll('path[data-code]') as NodeListOf<SVGPathElement>;
					
					allPaths.forEach((path) => {
						const code = path.getAttribute('data-code')?.toLowerCase();
						if (!code) return;
						
						const hasData = countriesWithData.has(code);
						
						if (hasData) {
							// Apply color for countries with data
							const value = countryData[code];
							if (value !== undefined) {
								const color = getColorForValue(value, maxMinutes);
								path.setAttribute('fill', color);
								path.style.fill = color;
								path.style.pointerEvents = 'auto';
								path.style.cursor = 'pointer';
							}
						} else {
							// Disable hover and interaction for countries without data
							path.style.pointerEvents = 'none';
							path.style.cursor = 'default';
							// Ensure it stays dark
							path.setAttribute('fill', '#1a1a1a');
							path.style.fill = '#1a1a1a';
						}
					});
				};
				
				// Try multiple times with increasing delays
				setTimeout(applyColorsAndDisableHover, 100);
				setTimeout(applyColorsAndDisableHover, 500);
				setTimeout(applyColorsAndDisableHover, 1000);
			}
		});
	});

	onDestroy(() => {
		if (mapInstance && mapInstance.destroy) {
			mapInstance.destroy();
		}
		mapInstance = null;
	});
</script>

<div class="world-map-container">
	<h3 class="mb-3">Listening by Country</h3>
	<div bind:this={mapContainer} class="world-map" style="width: 100%; height: 500px;"></div>
</div>

<style>
	.world-map-container {
		margin-bottom: 1rem;
	}

	.world-map {
		background-color: #0d1117;
		border-radius: 8px;
		overflow: hidden;
	}

	:global(.jvectormap-container) {
		background-color: #0d1117 !important;
	}

	:global(.jvectormap-legend-title) {
		color: #fff !important;
	}

	:global(.jvectormap-legend-tick) {
		color: #fff !important;
	}

	/* Style the tooltip to match page design */
	:global(.jvm-tooltip) {
		background-color: #181818 !important;
		border: 1px solid #404040 !important;
		border-radius: 8px !important;
		padding: 12px 16px !important;
		color: #ffffff !important;
		font-family: 'Inter', 'Helvetica Neue', Helvetica, Arial, sans-serif !important;
		font-size: 0.9rem !important;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4) !important;
		line-height: 1.6 !important;
		max-width: 250px !important;
		z-index: 10000 !important;
	}

	:global(.jvm-tooltip strong) {
		font-weight: 700 !important;
		font-size: 1rem !important;
		color: #ffffff !important;
		margin-bottom: 8px !important;
		display: block !important;
	}
</style>

