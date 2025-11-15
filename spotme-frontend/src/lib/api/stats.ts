import { apiRequest } from './client';

export interface ContentTypeBreakdown {
	audioTrackMinutes: number;
	audioTrackCount: number;
	podcastMinutes: number;
	podcastCount: number;
	audiobookMinutes: number;
	audiobookCount: number;
}

export interface Stats {
	totalMinutes: number;
	totalTracks: number;
	contentTypeBreakdown: ContentTypeBreakdown;
	uniqueArtists: number;
}

export interface StatsResponse {
	stats: Stats;
}

export const statsApi = {
	getStats: async (startDate?: string, endDate?: string): Promise<StatsResponse> => {
		const params = new URLSearchParams();
		if (startDate) params.append('startDate', startDate);
		if (endDate) params.append('endDate', endDate);
		
		const query = params.toString();
		return apiRequest<StatsResponse>(`/stats${query ? `?${query}` : ''}`);
	}
};

