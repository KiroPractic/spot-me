import { authStore } from '$lib/stores/auth';
import { browser } from '$app/environment';

const API_BASE_URL = browser 
	? (import.meta.env.VITE_API_URL || 'http://localhost:5002/api')
	: 'http://localhost:5002/api';

export interface ApiError {
	message: string;
	status: number;
}

async function handleResponse<T>(response: Response): Promise<T> {
	if (!response.ok) {
		const error: ApiError = {
			message: 'An error occurred',
			status: response.status
		};
		
		try {
			const data = await response.json();
			error.message = data.message || data.error || error.message;
		} catch {
			error.message = response.statusText || error.message;
		}
		
		throw error;
	}
	
	return response.json();
}

export async function apiRequest<T>(
	endpoint: string,
	options: RequestInit = {}
): Promise<T> {
	const token = authStore.getToken();
	
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
		...(options.headers as Record<string, string> || {})
	};
	
	if (token) {
		headers['Authorization'] = `Bearer ${token}`;
	}
	
	const response = await fetch(`${API_BASE_URL}${endpoint}`, {
		...options,
		headers
	});
	
	// Handle 401 - redirect to login
	if (response.status === 401 && browser) {
		authStore.clearToken();
		window.location.href = '/login';
	}
	
	return handleResponse<T>(response);
}

export async function apiRequestFormData<T>(
	endpoint: string,
	formData: FormData,
	options: RequestInit = {}
): Promise<T> {
	const token = authStore.getToken();
	
	const headers: Record<string, string> = {};
	
	if (token) {
		headers['Authorization'] = `Bearer ${token}`;
	}
	
	const response = await fetch(`${API_BASE_URL}${endpoint}`, {
		...options,
		method: options.method || 'POST',
		headers,
		body: formData
	});
	
	if (response.status === 401 && browser) {
		authStore.clearToken();
		window.location.href = '/login';
	}
	
	return handleResponse<T>(response);
}

