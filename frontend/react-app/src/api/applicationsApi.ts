import apiClient from './apiClient';
import type { JobResponse } from './jobsApi';

export interface ApplicationResponse {
  id: string;
  candidateId: string;
  jobId: string;
  job: JobResponse;
  status: string;
  appliedDate: string;
  resumeUrl?: string;
  coverLetter?: string;
}

export interface CreateApplicationRequest {
  jobId: string;
  resumeUrl?: string;
  coverLetter?: string;
}

export const applicationsApi = {
  getAll: async () => {
    const response = await apiClient.get<PagedResult<ApplicationResponse>>('/applications');
    return response.data.items;
  },
  getById: async (id: string) => {
    const response = await apiClient.get<ApplicationResponse>(`/applications/${id}`);
    return response.data;
  },
  create: async (data: CreateApplicationRequest) => {
    const response = await apiClient.post<ApplicationResponse>('/applications', data);
    return response.data;
  },
  withdraw: async (id: string) => {
    const response = await apiClient.post(`/applications/${id}/withdraw`);
    return response.data;
  }
};
