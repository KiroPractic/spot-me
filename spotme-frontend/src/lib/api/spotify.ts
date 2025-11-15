import { apiRequest } from './client';

export interface SpotifyAuthResponse {
	authUrl: string;
}

export interface PlaylistImage {
	url: string;
	height: number;
	width: number;
}

export interface Playlist {
	id: string;
	name: string;
	description?: string;
	images?: PlaylistImage[];
	tracks?: {
		total: number;
	};
	external_urls?: {
		spotify: string;
	};
	public?: boolean;
	collaborative?: boolean;
	followers?: {
		total: number;
	};
}

export interface PlaylistsResponse {
	playlists: Playlist[];
}

export const spotifyApi = {
	getAuthUrl: async (): Promise<SpotifyAuthResponse> => {
		return apiRequest<SpotifyAuthResponse>('/spotify/auth');
	},
	
	getPlaylists: async (): Promise<PlaylistsResponse> => {
		return apiRequest<PlaylistsResponse>('/spotify/playlists');
	}
};

