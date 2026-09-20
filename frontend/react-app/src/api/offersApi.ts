import apiClient from './apiClient';
import type { PagedResult } from './apiClient';

export interface OfferResponse {
  id: string;
  applicationId: string;
  jobTitle: string;
  candidateName: string;
  position: string;
  salary: number;
  employmentType?: string;
  startDate: string;
  expiryDate: string;
  status: string;
  additionalTerms?: string;
  createdAt: string;
}

export interface CreateOfferRequest {
  applicationId: string;
  position: string;
  salary: number;
  employmentType?: string;
  startDate: string;
  expiryDate: string;
  additionalTerms?: string;
}

export const offersApi = {
  getAll: async (applicationId?: string) => {
    const params = applicationId ? { applicationId } : {};
    const response = await apiClient.get<PagedResult<OfferResponse>>('/offers', { params });
    return response.data.items;
  },
  getById: async (id: string) => {
    const response = await apiClient.get<OfferResponse>(`/offers/${id}`);
    return response.data;
  },
  create: async (data: CreateOfferRequest) => {
    const response = await apiClient.post<OfferResponse>('/offers', data);
    return response.data;
  },
  updateStatus: async (id: string, status: string) => {
    const response = await apiClient.patch(`/offers/${id}/status`, { status });
    return response.data;
  }
};
