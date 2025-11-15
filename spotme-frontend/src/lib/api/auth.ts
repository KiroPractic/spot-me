import { apiRequest } from './client';

export interface LoginRequest {
	email: string;
	password: string;
}

export interface RegisterRequest {
	email: string;
	password: string;
}

export interface AuthResponse {
	token: string;
}

export const authApi = {
	login: async (data: LoginRequest): Promise<AuthResponse> => {
		return apiRequest<AuthResponse>('/auth/login', {
			method: 'POST',
			body: JSON.stringify(data)
		});
	},
	
	register: async (data: RegisterRequest): Promise<AuthResponse> => {
		return apiRequest<AuthResponse>('/auth/register', {
			method: 'POST',
			body: JSON.stringify(data)
		});
	}
};

