import { apiRequest } from './client';

export interface ContentTypeBreakdown {
	audioTrackMinutes: number;
	audioTrackCount: number;
	podcastMinutes: number;
	podcastCount: number;
	audiobookMinutes: number;
	audiobookCount: number;
}

export interface ArtistStats {
	artistName: string;
	playCount: number;
	totalMinutes: number;
	uniqueTracks: number;
	uniqueAlbums: number;
	primaryContentType: string;
	skipCount?: number;
	skipScore?: number;
}

export interface TrackStats {
	artistName: string;
	trackName: string;
	albumName?: string;
	spotifyUri?: string;
	playCount: number;
	totalMinutes: number;
	contentType: string;
	averagePlayDuration: number;
	mostCommonCompletionStatus: string;
	skipCount?: number;
	skipScore?: number;
}

export interface AlbumStats {
	artistName: string;
	albumName: string;
	playCount: number;
	totalMinutes: number;
	uniqueTracks: number;
}

export interface PlaybackBehavior {
	shufflePlays: number;
	skippedPlays: number;
	offlinePlays: number;
	incognitoPlays: number;
	startReasons: Record<string, number>;
	endReasons: Record<string, number>;
	completedPlays: number;
	partiallyCompletedPlays: number;
	barelyPlayedPlays: number;
	completionStatusBreakdown: Record<string, number>;
}

export interface MusicStats {
	totalMusicTracks: number;
	totalMusicMinutes: number;
	uniqueMusicArtists: number;
	uniqueMusicTracks: number;
	uniqueMusicAlbums: number;
	topMusicArtists?: ArtistStats[];
	topMusicTracks?: TrackStats[];
	topMusicAlbums?: AlbumStats[];
	topSkippedMusicTracks?: TrackStats[];
	topSkippedMusicArtists?: ArtistStats[];
	musicPlaybackBehavior?: PlaybackBehavior;
}

export interface DayOfWeekStats {
	dayOfWeek: number; // 0 = Sunday, 1 = Monday, etc.
	dayName: string;
	playCount: number;
	totalMinutes: number;
	averageMinutesPerOccurrence: number;
	averageMinutesPerDay: number;
}

export interface HourOfDayStats {
	hour: number;
	hourLabel: string;
	playCount: number;
	totalMinutes: number;
	averageMinutesPerOccurrence: number;
	averageMinutesPerDay: number;
}

export interface MonthlyStats {
	month: number;
	year: number;
	monthName: string;
	monthYearLabel: string;
	playCount: number;
	totalMinutes: number;
	averageMinutesPerDay: number;
}

export interface TimeBasedStats {
	dayOfWeekStats: DayOfWeekStats[];
	hourOfDayStats: HourOfDayStats[];
	monthlyStats: MonthlyStats[];
}

export interface CountryStats {
	countryCode: string;
	countryName: string;
	playCount: number;
	totalMinutes: number;
	uniqueTracks: number;
	uniqueArtists: number;
}

export interface PlatformBreakdown {
	platformUsage: Record<string, number>;
	platformMinutes: Record<string, number>;
}

export interface Stats {
	totalMinutes: number;
	totalTracks: number;
	contentTypeBreakdown: ContentTypeBreakdown;
	uniqueArtists: number;
	musicStats?: MusicStats;
	timeBasedStats?: TimeBasedStats;
	countryStats?: CountryStats[];
	platformBreakdown?: PlatformBreakdown;
	playbackBehavior?: PlaybackBehavior;
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

