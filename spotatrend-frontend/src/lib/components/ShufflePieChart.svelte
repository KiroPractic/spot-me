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
	import type { PlaybackBehavior } from '$lib/api/stats';

	export let data: PlaybackBehavior;

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
		if (!data) return;

		// Calculate shuffle vs non-shuffle
		const shufflePlays = data.shufflePlays || 0;
		// Total plays = completed + skipped (following same pattern as skipped chart)
		const completedPlays = data.completedPlays || 0;
		const skippedPlays = data.skippedPlays || 0;
		const totalPlays = completedPlays + skippedPlays;
		const nonShufflePlays = Math.max(0, totalPlays - shufflePlays);

		if (totalPlays === 0) {
			// No data to display
			return;
		}

		// Prepare data for pie chart - shuffle vs non-shuffle
		const labels: string[] = ['Shuffle', 'No Shuffle'];
		const values: number[] = [shufflePlays, nonShufflePlays];
		const colors: string[] = ['#1ED760', '#808080']; // Light green for shuffle, Gray for no shuffle

		const config: ChartConfiguration<'pie'> = {
			type: 'pie',
			data: {
				labels: labels,
				datasets: [
					{
						label: 'Number of Plays',
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
						text: 'Shuffle Usage',
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
								return `${label}: ${value.toLocaleString()} (${percentage}%)`;
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

