<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import {
		Chart,
		ArcElement,
		PieController,
		CategoryScale,
		LinearScale,
		Title,
		Tooltip,
		Legend,
		type ChartConfiguration
	} from 'chart.js';
	import type { PlatformBreakdown } from '$lib/api/stats';

	export let data: PlatformBreakdown;

	let chartCanvas: HTMLCanvasElement;
	let chartInstance: Chart | null = null;

	onMount(() => {
		// Register Chart.js components for pie chart
		Chart.register(
			ArcElement,
			PieController,
			CategoryScale,
			LinearScale,
			Title,
			Tooltip,
			Legend
		);

		if (chartCanvas && data) {
			createChart();
		}
	});

	function createChart() {
		if (!data || !data.platformMinutes) return;

		// Convert platform minutes to array and sort by value
		const platformEntries = Object.entries(data.platformMinutes)
			.map(([platform, minutes]) => ({ platform, minutes }))
			.filter(entry => entry.minutes > 0)
			.sort((a, b) => b.minutes - a.minutes);

		if (platformEntries.length === 0) {
			// No data to display
			return;
		}

		// Prepare data for pie chart
		const labels: string[] = platformEntries.map(entry => formatPlatformName(entry.platform));
		const values: number[] = platformEntries.map(entry => entry.minutes);
		
		// Generate colors for platforms - use a color palette
		const colorPalette = [
			'#1DB954', // Spotify green
			'#1ED760', // Lighter green
			'#00D4FF', // Cyan
			'#8B5CF6', // Purple
			'#F59E0B', // Amber
			'#EF4444', // Red
			'#3B82F6', // Blue
			'#10B981', // Emerald
			'#F97316', // Orange
			'#EC4899'  // Pink
		];
		const colors: string[] = platformEntries.map((_, index) => 
			colorPalette[index % colorPalette.length]
		);

		const config: ChartConfiguration<'pie'> = {
			type: 'pie',
			data: {
				labels: labels,
				datasets: [
					{
						label: 'Listening Time (minutes)',
						data: values,
						backgroundColor: colors,
						borderColor: '#181818',
						borderWidth: 2,
						hoverOffset: 8
					}
				]
			},
			options: {
				responsive: true,
				maintainAspectRatio: true,
				layout: {
					padding: {
						top: 0,
						bottom: 0
					}
				},
				plugins: {
					legend: {
						display: false
					},
					title: {
						display: true,
						text: 'Platform Usage',
						color: '#ffffff',
						font: {
							size: 18,
							weight: '700'
						},
						padding: {
							top: 0,
							bottom: 5
						}
					},
					tooltip: {
						backgroundColor: 'rgba(24, 24, 24, 0.95)',
						titleColor: '#ffffff',
						bodyColor: '#ffffff',
						borderColor: '#1DB954',
						borderWidth: 1,
						padding: {
							top: 14,
							right: 18,
							bottom: 14,
							left: 18
						},
						displayColors: true,
						boxPadding: 8,
						callbacks: {
							label: function(context) {
								const label = context.label || '';
								const value = context.parsed || 0;
								const total = values.reduce((a, b) => a + b, 0);
								const percentage = ((value / total) * 100).toFixed(1);
								const hours = Math.floor(value / 60);
								const minutes = Math.floor(value % 60);
								const timeLabel = hours > 0 
									? `${hours}h ${minutes}m` 
									: `${minutes}m`;
								return `${label}: ${timeLabel} (${percentage}%)`;
							}
						}
					}
				}
			}
		};

		if (chartCanvas) {
			chartInstance = new Chart(chartCanvas, config);
		}
	}

	function formatPlatformName(platform: string): string {
		// Format platform names to be more readable
		const platformMap: Record<string, string> = {
			'Android': 'Android',
			'iPhone': 'iPhone',
			'iPad': 'iPad',
			'Windows': 'Windows',
			'Mac': 'Mac',
			'Linux': 'Linux',
			'Web Player': 'Web Player',
			'Desktop': 'Desktop',
			'Mobile': 'Mobile',
			'Tablet': 'Tablet'
		};
		
		// Check if we have a mapping
		if (platformMap[platform]) {
			return platformMap[platform];
		}
		
		// Otherwise, capitalize first letter of each word
		return platform
			.split(/(?=[A-Z])/)
			.map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
			.join(' ');
	}

	onDestroy(() => {
		if (chartInstance) {
			chartInstance.destroy();
			chartInstance = null;
		}
	});

	// Update chart when data changes
	$: if (chartInstance && data) {
		chartInstance.destroy();
		createChart();
	} else if (!chartInstance && data && chartCanvas) {
		createChart();
	}
</script>

<div class="chart-container">
	<canvas bind:this={chartCanvas}></canvas>
</div>
<style>
	.chart-container {
		position: relative;
		width: 33.333%;
		background-color: var(--dark-card);
		border-radius: 8px;
		padding: 1.5rem;
		margin-bottom: 2rem;
	}
</style>

