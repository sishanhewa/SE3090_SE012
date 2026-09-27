import apiClient from './apiClient';
import type { PagedResult } from './apiClient';

export interface ApplicationResponse {
  id: string;
  jobId: string;
  jobTitle: string;
  companyName: string;
  candidateProfileId: string;
  candidateName: string;
  status: string;
  submittedAt: string;
  coverLetter?: string;
  aiScore?: number;
  aiRecommendation?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateApplicationRequest {
  jobId: string;
  resumeDocumentId?: string;
  coverLetter?: string;
}

export const applicationsApi = {
  getAll: async (jobId?: string) => {
    const query = jobId ? `?jobId=${jobId}` : '';
    const response = await apiClient.get<PagedResult<ApplicationResponse>>(`/applications${query}`);
    return response.data.items;
  },
  getMine: async () => {
    const response = await apiClient.get<PagedResult<ApplicationResponse>>('/applications/mine');
    return response.data.items;
  },
  getById: async (id: string) => {
    const response = await apiClient.get<ApplicationResponse>(`/applications/${id}`);
    return response.data;
  },
  create: async (jobId: string, data: CreateApplicationRequest) => {
    const response = await apiClient.post<ApplicationResponse>(`/applications/jobs/${jobId}`, data);
    return response.data;
  },
  updateStatus: async (id: string, status: string, notes?: string) => {
    const response = await apiClient.patch(`/applications/${id}/status`, { status, notes });
    return response.data;
  },
  withdraw: async (id: string) => {
    const response = await apiClient.post(`/applications/${id}/withdraw`);
    return response.data;
  }
};
