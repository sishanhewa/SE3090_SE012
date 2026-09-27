import apiClient from './apiClient';
import type { PagedResult } from './apiClient';

export interface EmployeeResponse {
  id: string;
  userId: string;
  name: string;
  companyId: string;
  departmentId: string;
  employeeNumber: string;
  position: string;
  startDate: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEmployeeRequest {
  userId: string;
  departmentId: string;
  employeeNumber: string;
  position: string;
  startDate: string;
  applicationId?: string;
}

export const employeesApi = {
  getAll: async (companyId: string) => {
    const response = await apiClient.get<PagedResult<EmployeeResponse>>(`/companies/${companyId}/employees`);
    return response.data;
  },
  getById: async (id: string, companyId: string) => {
    const response = await apiClient.get<EmployeeResponse>(`/companies/${companyId}/employees/${id}`);
    return response.data;
  },
  create: async (companyId: string, data: CreateEmployeeRequest) => {
    const response = await apiClient.post<EmployeeResponse>(`/companies/${companyId}/employees`, data);
    return response.data;
  },
  update: async (id: string, companyId: string, data: any) => {
    const response = await apiClient.put<EmployeeResponse>(`/companies/${companyId}/employees/${id}`, data);
    return response.data;
  }
};
