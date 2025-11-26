<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { authApi } from '$lib/api/auth';
	import { type ApiError } from '$lib/api/client';
	
	export let params: Record<string, string> = {};
	
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
			const response = await authApi.register({ email, password });
			authStore.setToken(response.token);
			goto('/data');
		} catch (err) {
			const apiError = err as ApiError;
			error = apiError.message || 'Registration failed. Please check your input.';
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
			<h1 class="mb-4 text-center">Create Account</h1>
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
					<small class="form-text">Password must be at least 8 characters with uppercase, lowercase, digit, and special character</small>
				</div>
				{#if error}
					<div class="alert alert-danger">{error}</div>
				{/if}
				<button type="submit" class="btn btn-primary spotify-button w-100 mb-3 mt-4" disabled={loading}>
					{#if loading}
						<span class="spinner-border spinner-border-sm me-2"></span>
					{:else}
						<i class="bi bi-person-plus me-2"></i>
					{/if}
					Register
				</button>
			</form>
			<p class="text-left mt-3">
				Already have an account? <a href="/login">Sign in here</a>
			</p>
		</div>
	</div>
</div>

