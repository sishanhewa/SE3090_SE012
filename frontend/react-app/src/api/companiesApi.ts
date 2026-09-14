import apiClient from './apiClient';

export interface CompanyResponse {
  id: string;
  name: string;
  industry: string;
  location: string;
  contactEmail: string;
  contactPhone?: string;
  website?: string;
  logoUrl?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCompanyRequest {
  name: string;
  industry: string;
  location: string;
  contactEmail: string;
  contactPhone?: string;
  website?: string;
}

export const companiesApi = {
  getAll: async () => {
    const response = await apiClient.get<CompanyResponse[]>('/companies');
    return response.data;
  },
  getById: async (id: string) => {
    const response = await apiClient.get<CompanyResponse>(`/companies/${id}`);
    return response.data;
  },
  create: async (data: CreateCompanyRequest) => {
    const response = await apiClient.post<CompanyResponse>('/companies', data);
    return response.data;
  },
  update: async (id: string, data: Partial<CreateCompanyRequest>) => {
    const response = await apiClient.put<CompanyResponse>(`/companies/${id}`, data);
    return response.data;
  }
};
