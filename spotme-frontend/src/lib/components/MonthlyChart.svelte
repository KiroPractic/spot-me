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
	import type { MonthlyStats } from '$lib/api/stats';

	export let data: MonthlyStats[] = [];

	let chartCanvas: HTMLCanvasElement;
	let chartInstance: Chart | null = null;
	let dataMap: Map<number, MonthlyStats> = new Map();

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

		// Create a map for quick lookup using month number as key
		dataMap = new Map(data.map(item => [item.month, item]));
		
		// Month names in order
		const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 
			'July', 'August', 'September', 'October', 'November', 'December'];
		
		// Build ordered arrays for all 12 months, filling in missing months with 0
		const labels: string[] = [];
		const minutes: number[] = [];
		
		for (let month = 1; month <= 12; month++) {
			const stat = dataMap.get(month);
			labels.push(monthNames[month - 1]);
			minutes.push(stat ? stat.averageMinutesPerDay : 0);
		}

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
						text: 'Average Listening Time by Month (All Years)',
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
								const monthIndex = context.dataIndex + 1; // Month is 1-12
								const stat = dataMap.get(monthIndex);
								const avgMinutes = context.parsed.y;
								const avgDays = Math.floor(avgMinutes / (24 * 60));
								const avgHours = Math.floor((avgMinutes % (24 * 60)) / 60);
								const avgMins = Math.floor(avgMinutes % 60);
								
								if (stat && stat.totalMinutes > 0) {
									const totalMinutes = stat.totalMinutes;
									const totalDays = Math.floor(totalMinutes / (24 * 60));
									const totalHours = Math.floor((totalMinutes % (24 * 60)) / 60);
									const totalMins = Math.floor(totalMinutes % 60);
									
									let avgText: string;
									if (avgDays > 0) {
										avgText = `${avgDays}d ${avgHours}h ${avgMins}m`;
									} else if (avgHours > 0) {
										avgText = avgMins > 0 ? `${avgHours}h ${avgMins}m` : `${avgHours}h`;
									} else {
										avgText = `${avgMins}m`;
									}
									
									let totalText: string;
									if (totalDays > 0) {
										totalText = `${totalDays}d ${totalHours}h ${totalMins}m`;
									} else if (totalHours > 0) {
										totalText = totalMins > 0 ? `${totalHours}h ${totalMins}m` : `${totalHours}h`;
									} else {
										totalText = `${totalMins}m`;
									}
									
									return [
										`Average: ${avgText}`,
										`Total: ${totalText}`
									];
								}
								
								let avgText: string;
								if (avgDays > 0) {
									avgText = `${avgDays}d ${avgHours}h ${avgMins}m`;
								} else if (avgHours > 0) {
									avgText = avgMins > 0 ? `${avgHours}h ${avgMins}m` : `${avgHours}h`;
								} else {
									avgText = `${avgMins}m`;
								}
								return `Average: ${avgText}`;
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
		// Create a map for quick lookup using month number as key
		dataMap = new Map(data.map(item => [item.month, item]));
		
		// Month names in order
		const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 
			'July', 'August', 'September', 'October', 'November', 'December'];
		
		const labels: string[] = [];
		const minutes: number[] = [];
		
		for (let month = 1; month <= 12; month++) {
			const stat = dataMap.get(month);
			labels.push(monthNames[month - 1]);
			minutes.push(stat ? stat.averageMinutesPerDay : 0);
		}

		chartInstance.data.labels = labels;
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

