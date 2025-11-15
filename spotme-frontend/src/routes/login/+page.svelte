<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { authApi } from '$lib/api/auth';
	import { type ApiError } from '$lib/api/client';
	
	let email = '';
	let password = '';
	let showPassword = false;
	let error = '';
	let loading = false;
	
	onMount(() => {
		if ($authStore.isAuthenticated) {
			goto('/data');
		}
	});
	
	async function handleSubmit(event: Event) {
		event.preventDefault();
		error = '';
		loading = true;
		
		try {
			const response = await authApi.login({ email, password });
			authStore.setToken(response.token);
			goto('/data');
		} catch (err) {
			const apiError = err as ApiError;
			error = apiError.message || 'Invalid email or password';
		} finally {
			loading = false;
		}
	}
	
	function togglePasswordVisibility() {
		showPassword = !showPassword;
	}
</script>

<div class="dark-container">
	<div class="spotify-content">
		<div class="auth-card">
			<h1 class="mb-4 text-center">Sign In to SpotMe</h1>
			<form on:submit={handleSubmit}>
				<div class="mb-3">
					<label for="email" class="form-label">Email</label>
					<input 
						type="email" 
						class="form-control dark-input" 
						id="email" 
						bind:value={email}
						required 
						disabled={loading}
					/>
				</div>
				<div class="mb-3">
					<label for="password" class="form-label">Password</label>
					<div class="input-group">
						<input 
							class="form-control dark-input" 
							id="password" 
							bind:value={password}
							{...{ type: showPassword ? 'text' : 'password' }}
							required 
							disabled={loading}
						/>
						<button 
							class="btn btn-outline-secondary" 
							type="button" 
							on:click={togglePasswordVisibility}
						>
							<i class="bi" class:bi-eye={!showPassword} class:bi-eye-slash={showPassword}></i>
						</button>
					</div>
				</div>
				{#if error}
					<div class="alert alert-danger">{error}</div>
				{/if}
				<button type="submit" class="btn btn-primary spotify-button w-100 mb-3 mt-4" disabled={loading}>
					{#if loading}
						<span class="spinner-border spinner-border-sm me-2"></span>
					{:else}
						<i class="bi bi-box-arrow-in-right me-2"></i>
					{/if}
					Sign In
				</button>
			</form>
			<p class="text-left mt-3">
				Don't have an account? <a href="/register">Register here</a>
			</p>
		</div>
	</div>
</div>

