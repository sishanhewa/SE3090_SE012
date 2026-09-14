import apiClient from './apiClient';

export interface ApplicationResponse {
  id: string;
  candidateId: string;
  jobId: string;
  job?: any;
  status: string;
  appliedDate: string;
}

export const applicationsApi = {
  getAll: async () => {
    const response = await apiClient.get<ApplicationResponse[]>('/applications');
    return response.data;
  }
};
