<script lang="ts">
	import '../app.css';
	import { browser } from '$app/environment';
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { authStore } from '$lib/stores/auth';
	import Navbar from '$lib/components/Navbar.svelte';
	import Footer from '$lib/components/Footer.svelte';
	
	export let params: Record<string, string> = {};
	
	$: isAuthenticated = $authStore.isAuthenticated;

	// Load jsvectormap CSS only on client side
	onMount(async () => {
		if (browser) {
			await import('jsvectormap/dist/jsvectormap.css');
		}
	});
</script>

<div class="layout-wrapper">
	<Navbar />
	<main>
		<slot />
	</main>
	<Footer />
</div>

<style>
	.layout-wrapper {
		display: flex;
		flex-direction: column;
		min-height: 100vh;
	}

	main {
		flex: 1;
		padding-bottom: 3rem;
	}
</style>

