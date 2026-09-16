import apiClient from './apiClient';

export interface JobResponse {
  id: string;
  companyId: string;
  title: string;
  description: string;
  department: string;
  location: string;
  employmentType: string;
  experienceLevel: string;
  status: string;
  salaryMin?: number;
  salaryMax?: number;
  postedDate?: string;
}

export interface CreateJobRequest {
  companyId: string;
  title: string;
  description: string;
  department: string;
  location: string;
  employmentType: string;
  experienceLevel: string;
  salaryMin?: number;
  salaryMax?: number;
}

export const jobsApi = {
  getAll: async () => {
    const response = await apiClient.get<PagedResult<JobResponse>>('/jobs');
    return response.data.items;
  },
  getById: async (id: string) => {
    const response = await apiClient.get<JobResponse>(`/jobs/${id}`);
    return response.data;
  },
  create: async (data: CreateJobRequest) => {
    const response = await apiClient.post<JobResponse>('/jobs', data);
    return response.data;
  },
  update: async (id: string, data: Partial<CreateJobRequest>) => {
    const response = await apiClient.put<JobResponse>(`/jobs/${id}`, data);
    return response.data;
  },
  publish: async (id: string) => {
    const response = await apiClient.post(`/jobs/${id}/publish`);
    return response.data;
  },
  close: async (id: string) => {
    const response = await apiClient.post(`/jobs/${id}/close`);
    return response.data;
  }
};
