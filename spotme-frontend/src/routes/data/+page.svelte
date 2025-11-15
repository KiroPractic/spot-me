<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { dataApi, type FileInfo } from '$lib/api/data';
	import { type ApiError } from '$lib/api/client';
	
	let files: FileInfo[] = [];
	let loading = true;
	let uploading = false;
	let uploadProgress: { fileName: string; status: 'uploading' | 'success' | 'error'; message: string }[] = [];
	let selectedFiles: File[] = [];
	let showInstructions = true;
	
	onMount(() => {
		if (!$authStore.isAuthenticated) {
			goto('/login');
			return;
		}
		loadFiles();
	});
	
	async function loadFiles() {
		try {
			const response = await dataApi.getFiles();
			files = response.files;
		} catch (error) {
			console.error('Failed to load files:', error);
		} finally {
			loading = false;
		}
	}
	
	function handleFileSelect(event: Event) {
		const target = event.target as HTMLInputElement;
		if (target.files && target.files.length > 0) {
			selectedFiles = Array.from(target.files);
			uploadProgress = [];
		}
	}
	
	async function handleUpload(event: Event) {
		event.preventDefault();
		if (selectedFiles.length === 0) return;
		
		uploading = true;
		uploadProgress = selectedFiles.map(file => ({
			fileName: file.name,
			status: 'uploading' as const,
			message: 'Uploading...'
		}));
		
		const results: { fileName: string; status: 'success' | 'error'; message: string }[] = [];
		
		// Upload files sequentially
		for (const file of selectedFiles) {
			try {
				await dataApi.uploadFile(file);
				results.push({
					fileName: file.name,
					status: 'success',
					message: 'Uploaded successfully'
				});
			} catch (error) {
				const apiError = error as ApiError;
				results.push({
					fileName: file.name,
					status: 'error',
					message: apiError.message || 'Upload failed'
				});
			}
		}
		
		uploadProgress = results;
		selectedFiles = [];
		
		// Reset file input
		const fileInput = document.getElementById('file') as HTMLInputElement;
		if (fileInput) fileInput.value = '';
		
		// Reload files list
		await loadFiles();
		
		// Clear progress after 5 seconds
		setTimeout(() => {
			uploadProgress = [];
		}, 5000);
		
		uploading = false;
	}
	
	async function handleDelete(fileName: string) {
		if (!confirm(`Are you sure you want to delete all your streaming history data? This action cannot be undone.`)) return;
		
		try {
			await dataApi.deleteFile(fileName);
			await loadFiles();
		} catch (error) {
			console.error('Failed to delete file:', error);
			alert('Failed to delete data');
		}
	}
	
	function formatFileSize(bytes: number): string {
		if (bytes === 0) return '0 Bytes';
		const k = 1024;
		const sizes = ['Bytes', 'KB', 'MB', 'GB'];
		const i = Math.floor(Math.log(bytes) / Math.log(k));
		return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
	}
</script>

<div class="container mt-4">
	<h1 class="mb-4">My Data</h1>
	
	<!-- Instructions Card -->
	{#if showInstructions}
		<div class="card mb-4 instruction-card">
			<div class="card-header">
				<h5 class="mb-0">
					<i class="bi bi-info-circle me-2"></i>
					How to Get Your Spotify Data
				</h5>
			</div>
			<div class="card-body">
				<ol class="instruction-list">
					<li>
						<strong>Visit Spotify's Privacy Settings:</strong>
						<br>
						Go to <a href="https://www.spotify.com/account/privacy/" target="_blank" rel="noopener noreferrer">spotify.com/account/privacy</a> and log in to your account.
					</li>
					<li>
						<strong>Request Your Extended Streaming History:</strong>
						<br>
						Scroll down to "Download your data" and click "Request data". Select "Extended streaming history" and submit your request.
					</li>
					<li>
						<strong>Wait for Email:</strong>
						<br>
						Spotify will email you (usually within a few days) with a link to download your data as a ZIP file.
					</li>
					<li>
						<strong>Extract and Upload:</strong>
						<br>
						Download and extract the ZIP file. Look for JSON files named like <code>Streaming_History_Audio_*.json</code> or <code>Streaming_History_Video_*.json</code>. Upload these files below.
					</li>
				</ol>
				<div class="mt-3">
					<button 
						class="btn btn-sm btn-outline-secondary" 
						on:click={() => showInstructions = false}
					>
						Hide Instructions
					</button>
				</div>
			</div>
		</div>
	{:else}
		<div class="mb-4">
			<button 
				class="btn btn-sm btn-outline-secondary" 
				on:click={() => showInstructions = true}
			>
				<i class="bi bi-info-circle me-2"></i>
				Show Instructions
			</button>
		</div>
	{/if}
	
	<!-- Upload Section -->
	<div class="card mb-4 upload-card">
		<div class="card-header">
			<h5 class="card-title mb-0">
				<i class="bi bi-cloud-upload me-2"></i>
				Upload Streaming History Files
			</h5>
		</div>
		<div class="card-body">
			<form on:submit={handleUpload}>
				<div class="mb-3">
					<label for="file" class="form-label">Select JSON Files (You can select multiple files)</label>
					<input 
						type="file" 
						class="form-control dark-input" 
						id="file" 
						accept=".json" 
						multiple
						on:change={handleFileSelect}
						disabled={uploading}
					/>
					<small class="form-text">You can upload multiple files at once. Each file will be processed separately.</small>
				</div>
				
				{#if selectedFiles.length > 0}
					<div class="mb-3">
						<strong>Selected Files ({selectedFiles.length}):</strong>
						<div class="mt-2">
							{#each selectedFiles as file}
								<div class="file-item-preview d-flex justify-content-between align-items-center p-2 mb-2 rounded">
									<span>
										<i class="bi bi-file-earmark-code me-2"></i>
										{file.name}
									</span>
									<small class="text-muted">{formatFileSize(file.size)}</small>
								</div>
							{/each}
						</div>
					</div>
				{/if}
				
				{#if uploadProgress.length > 0}
					<div class="mb-3">
						<strong>Upload Progress:</strong>
						{#each uploadProgress as progress}
							<div class="alert {progress.status === 'success' ? 'alert-success' : progress.status === 'error' ? 'alert-danger' : 'alert-info'} mb-2 py-2">
								<div class="d-flex align-items-center">
									{#if progress.status === 'uploading'}
										<span class="spinner-border spinner-border-sm me-2" role="status"></span>
									{:else if progress.status === 'success'}
										<i class="bi bi-check-circle me-2"></i>
									{:else}
										<i class="bi bi-x-circle me-2"></i>
									{/if}
									<span><strong>{progress.fileName}:</strong> {progress.message}</span>
								</div>
							</div>
						{/each}
					</div>
				{/if}
				
				<button type="submit" class="btn btn-primary" disabled={uploading || selectedFiles.length === 0}>
					{#if uploading}
						<span class="spinner-border spinner-border-sm me-2"></span>
						Uploading...
					{:else}
						<i class="bi bi-cloud-upload me-2"></i>
						Upload {selectedFiles.length > 0 ? `${selectedFiles.length} File${selectedFiles.length > 1 ? 's' : ''}` : 'Files'}
					{/if}
				</button>
			</form>
		</div>
	</div>
	
	<!-- Uploaded Files Section -->
	<div class="card">
		<div class="card-header">
			<h5 class="card-title mb-0">
				<i class="bi bi-folder me-2"></i>
				Your Data
			</h5>
		</div>
		<div class="card-body p-0">
			{#if loading}
				<div class="text-center py-4">
					<div class="spinner-border" role="status">
						<span class="visually-hidden">Loading...</span>
					</div>
				</div>
			{:else if files.length > 0}
				<div class="data-files-list">
					{#each files as file}
						<div class="data-file-item">
							<div class="data-file-info">
								<div class="data-file-name">
									<i class="bi bi-database me-2"></i>
									<strong>{file.fileName}</strong>
								</div>
								<div class="data-file-meta">
									<span class="data-file-entries">
										<i class="bi bi-music-note-list me-1"></i>
										{file.entryCount.toLocaleString()} entries
									</span>
									{#if file.dateRange}
										<span class="data-file-dates">
											<i class="bi bi-calendar-range me-1"></i>
											{file.dateRange}
										</span>
									{/if}
								</div>
							</div>
							<div class="data-file-actions">
								<button 
									class="btn btn-danger btn-sm" 
									on:click={() => handleDelete(file.fileName)}
									title="Delete all streaming history data"
								>
									<i class="bi bi-trash me-1"></i>
									Delete
								</button>
							</div>
						</div>
					{/each}
				</div>
			{:else}
				<div class="alert alert-info m-3 mb-0">
					<i class="bi bi-info-circle me-2"></i>
					No data uploaded yet. Follow the instructions above to download and upload your Spotify streaming history JSON files.
				</div>
			{/if}
		</div>
	</div>
</div>

