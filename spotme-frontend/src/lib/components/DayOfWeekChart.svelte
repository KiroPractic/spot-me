<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import {
		Chart,
		LineController,
		CategoryScale,
		LinearScale,
		PointElement,
		LineElement,
		Title,
		Tooltip,
		Legend,
		type ChartConfiguration
	} from 'chart.js';
	import type { DayOfWeekStats } from '$lib/api/stats';

	export let data: DayOfWeekStats[] = [];

	let chartCanvas: HTMLCanvasElement;
	let chartInstance: Chart | null = null;

	// Day order: Monday (1) first, then Tuesday through Sunday (0)
	// This ensures Monday appears first in the chart
	const dayOrder = [1, 2, 3, 4, 5, 6, 0]; // Monday=1, Tuesday=2, ..., Sunday=0
	const dayLabels = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

	onMount(() => {
		// Register Chart.js components - LineController is required for line charts
		Chart.register(
			LineController,
			CategoryScale,
			LinearScale,
			PointElement,
			LineElement,
			Title,
			Tooltip,
			Legend
		);

		// Create a map for quick lookup (sorting doesn't affect display order)
		const dataMap = new Map(data.map(item => [item.dayOfWeek, item]));
		
		// Build ordered arrays starting with Monday, filling in missing days with 0
		const labels: string[] = [];
		const minutes: number[] = [];
		
		// Iterate through dayOrder to ensure Monday is first
		dayOrder.forEach((day, index) => {
			const stat = dataMap.get(day);
			labels.push(dayLabels[index]); // Use index to get correct label (Monday first)
			minutes.push(stat ? stat.averageMinutesPerDay : 0);
		});

		const config: ChartConfiguration<'line'> = {
			type: 'line',
			data: {
				labels: labels,
				datasets: [
					{
						label: 'Average Listening Time (minutes)',
						data: minutes,
						borderColor: '#1DB954', // Spotify green
						backgroundColor: 'rgba(29, 185, 84, 0.1)',
						borderWidth: 3,
						tension: 0.4,
						fill: true,
						pointBackgroundColor: '#1DB954',
						pointBorderColor: '#ffffff',
						pointBorderWidth: 2,
						pointRadius: 6,
						pointHoverRadius: 8,
						pointHoverBackgroundColor: '#1ED760',
						pointHoverBorderColor: '#ffffff',
						pointHoverBorderWidth: 3
					}
				]
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				plugins: {
					legend: {
						display: true,
						position: 'top',
						labels: {
							color: '#ffffff',
							font: {
								size: 14,
								weight: '500'
							},
							padding: 15
						}
					},
					title: {
						display: true,
						text: 'Average Listening Time by Day of Week',
						color: '#ffffff',
						font: {
							size: 18,
							weight: '700'
						},
						padding: {
							top: 10,
							bottom: 20
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
								const minutes = context.parsed.y;
								const hours = Math.floor(minutes / 60);
								const mins = Math.floor(minutes % 60);
								return `${hours}h ${mins}m`;
							}
						}
					}
				},
				scales: {
					x: {
						grid: {
							color: 'rgba(255, 255, 255, 0.1)',
							drawBorder: false
						},
						ticks: {
							color: '#b3b3b3',
							font: {
								size: 12,
								weight: '500'
							}
						}
					},
					y: {
						grid: {
							color: 'rgba(255, 255, 255, 0.1)',
							drawBorder: false
						},
						ticks: {
							color: '#b3b3b3',
							font: {
								size: 12,
								weight: '500'
							},
							callback: function(value) {
								const minutes = Number(value);
								const hours = Math.floor(minutes / 60);
								const mins = Math.floor(minutes % 60);
								if (hours > 0) {
									return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
								}
								return mins > 0 ? `0h ${mins}m` : '0h';
							}
						},
						beginAtZero: true
					}
				}
			}
		};

		if (chartCanvas) {
			chartInstance = new Chart(chartCanvas, config);
		}
	});

	onDestroy(() => {
		if (chartInstance) {
			chartInstance.destroy();
			chartInstance = null;
		}
	});

	// Update chart when data changes
	$: if (chartInstance && data.length > 0) {
		// Create a map for quick lookup
		const dataMap = new Map(data.map(item => [item.dayOfWeek, item]));
		
		const minutes: number[] = [];
		dayOrder.forEach((day) => {
			const stat = dataMap.get(day);
			minutes.push(stat ? stat.averageMinutesPerDay : 0);
		});

		chartInstance.data.datasets[0].data = minutes;
		chartInstance.update();
	}
</script>

<div class="chart-container">
	<canvas bind:this={chartCanvas}></canvas>
</div>

<style>
	.chart-container {
		position: relative;
		height: 400px;
		width: 100%;
		background-color: var(--dark-card);
		border-radius: 8px;
		padding: 1.5rem;
		margin-bottom: 2rem;
	}
</style>

