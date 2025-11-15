<script lang="ts">
	import '../app.css';
	import { page } from '$app/stores';
	import { authStore } from '$lib/stores/auth';
	import Navbar from '$lib/components/Navbar.svelte';
	
	$: isAuthenticated = $authStore.isAuthenticated;
</script>

<Navbar />
<main>
	<slot />
</main>

