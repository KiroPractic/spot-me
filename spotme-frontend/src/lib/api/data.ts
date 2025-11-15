import { apiRequest, apiRequestFormData } from './client';

export interface FileInfo {
	fileId?: string;
	fileName: string;
	entryCount: number;
	dateRange?: string;
	fileSize?: number;
	uploadedAt?: string;
	isDatabaseEntry?: boolean;
}

export interface FilesResponse {
	files: FileInfo[];
}

export interface UploadResponse {
	message: string;
}

export interface DeleteFileResponse {
	message: string;
}

export const dataApi = {
	getFiles: async (): Promise<FilesResponse> => {
		return apiRequest<FilesResponse>('/data/files');
	},
	
	uploadFile: async (file: File): Promise<UploadResponse> => {
		const formData = new FormData();
		formData.append('file', file);
		return apiRequestFormData<UploadResponse>('/data/upload', formData);
	},
	
	deleteFile: async (fileId?: string, fileName?: string): Promise<DeleteFileResponse> => {
		return apiRequest<DeleteFileResponse>('/data/delete', {
			method: 'POST',
			body: JSON.stringify({ fileId, fileName })
		});
	}
};

