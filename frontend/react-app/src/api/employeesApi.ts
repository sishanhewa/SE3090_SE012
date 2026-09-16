import apiClient from './apiClient';

export interface EmployeeResponse {
  id: string;
  userId: string;
  companyId: string;
  department: string;
  position: string;
  startDate: string;
  status: string;
  salary: number;
}

export interface CreateEmployeeRequest {
  userId: string;
  companyId: string;
  department: string;
  position: string;
  startDate: string;
  salary: number;
}

export const employeesApi = {
  getAll: async () => {
    const response = await apiClient.get<PagedResult<EmployeeResponse>>('/employees');
    return response.data.items;
  },
  getById: async (id: string) => {
    const response = await apiClient.get<EmployeeResponse>(`/employees/${id}`);
    return response.data;
  },
  create: async (data: CreateEmployeeRequest) => {
    const response = await apiClient.post<EmployeeResponse>('/employees', data);
    return response.data;
  },
  update: async (id: string, data: Partial<CreateEmployeeRequest>) => {
    const response = await apiClient.put<EmployeeResponse>(`/employees/${id}`, data);
    return response.data;
  },
  terminate: async (id: string) => {
    const response = await apiClient.post(`/employees/${id}/terminate`);
    return response.data;
  }
};
