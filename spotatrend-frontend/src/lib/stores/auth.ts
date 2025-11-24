import { writable } from 'svelte/store';
import { browser } from '$app/environment';

interface AuthState {
	token: string | null;
	isAuthenticated: boolean;
}

const createAuthStore = () => {
	const { subscribe, set, update } = writable<AuthState>({
		token: null,
		isAuthenticated: false
	});

	// Initialize from localStorage on client side
	if (browser) {
		const token = localStorage.getItem('jwt_token');
		if (token) {
			set({ token, isAuthenticated: true });
		}
	}

	return {
		subscribe,
		setToken: (token: string) => {
			if (browser) {
				localStorage.setItem('jwt_token', token);
			}
			set({ token, isAuthenticated: true });
		},
		clearToken: () => {
			if (browser) {
				localStorage.removeItem('jwt_token');
			}
			set({ token: null, isAuthenticated: false });
		},
		getToken: () => {
			let token: string | null = null;
			update((state) => {
				token = state.token;
				return state;
			});
			return token;
		}
	};
};

export const authStore = createAuthStore();

