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
	import type { HourOfDayStats } from '$lib/api/stats';

	export let data: HourOfDayStats[] = [];

	let chartCanvas: HTMLCanvasElement;
	let chartInstance: Chart | null = null;
	let dataMap: Map<number, HourOfDayStats> = new Map();

	onMount(() => {
		// Register Chart.js components
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

		// Create a map for quick lookup
		dataMap = new Map(data.map(item => [item.hour, item]));
		
		// Build ordered arrays for all 24 hours, filling in missing hours with 0
		const labels: string[] = [];
		const minutes: number[] = [];
		
		// Iterate through all 24 hours (0-23)
		for (let hour = 0; hour < 24; hour++) {
			const stat = dataMap.get(hour);
			labels.push(stat ? stat.hourLabel : `${hour.toString().padStart(2, '0')}:00`);
			minutes.push(stat ? stat.totalMinutes : 0);
		}

		const config: ChartConfiguration<'line'> = {
			type: 'line',
			data: {
				labels: labels,
				datasets: [
					{
						label: 'Total Listening Time (minutes)',
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
						text: 'Listening Time by Hour of Day',
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
								const hour = context.dataIndex;
								const stat = dataMap.get(hour);
								const totalMinutes = context.parsed.y;
								const totalHours = Math.floor(totalMinutes / 60);
								const totalMins = Math.floor(totalMinutes % 60);
								
								if (stat && stat.averageMinutesPerDay > 0) {
									const avgMinutes = stat.averageMinutesPerDay;
									const avgHours = Math.floor(avgMinutes / 60);
									const avgMins = Math.floor(avgMinutes % 60);
									
									if (avgHours > 0) {
										return [
											`Total: ${totalHours}h ${totalMins}m`,
											`Average: ${avgHours}h ${avgMins}m`
										];
									} else {
										return [
											`Total: ${totalHours}h ${totalMins}m`,
											`Average: ${avgMins}m`
										];
									}
								}
								return `Total: ${totalHours}h ${totalMins}m`;
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
							},
							maxRotation: 45,
							minRotation: 45
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
		dataMap = new Map(data.map(item => [item.hour, item]));
		
		const minutes: number[] = [];
		for (let hour = 0; hour < 24; hour++) {
			const stat = dataMap.get(hour);
			minutes.push(stat ? stat.totalMinutes : 0);
		}

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

