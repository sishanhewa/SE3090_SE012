import apiClient from './apiClient';

export interface OnboardingTemplate {
  id: string;
  name: string;
  description?: string;
  companyId: string;
  tasks: OnboardingTask[];
}

export interface OnboardingTask {
  id: string;
  title: string;
  description?: string;
  sortOrder: number;
  isMandatory: boolean;
}

export interface EmployeeOnboardingTask {
  id: string;
  onboardingTaskId: string;
  taskTitle: string;
  taskDescription?: string;
  isMandatory: boolean;
  isCompleted: boolean;
  completedAt?: string;
  notes?: string;
}

export interface CreateOnboardingTemplateRequest {
  name: string;
  description?: string;
  tasks: { title: string; description?: string; sortOrder: number; isMandatory: boolean }[];
}

export const onboardingApi = {
  getTemplates: async (companyId: string): Promise<OnboardingTemplate[]> => {
    const response = await apiClient.get<OnboardingTemplate[]>(
      `/companies/${companyId}/onboarding/templates`
    );
    return response.data;
  },

  createTemplate: async (
    companyId: string,
    data: CreateOnboardingTemplateRequest
  ): Promise<OnboardingTemplate> => {
    const response = await apiClient.post<OnboardingTemplate>(
      `/companies/${companyId}/onboarding/templates`,
      data
    );
    return response.data;
  },

  getEmployeeTasks: async (employeeId: string): Promise<EmployeeOnboardingTask[]> => {
    const response = await apiClient.get<EmployeeOnboardingTask[]>(
      `/employees/${employeeId}/onboarding`
    );
    return response.data;
  },

  completeTask: async (
    employeeId: string,
    taskId: string,
    notes?: string
  ): Promise<void> => {
    await apiClient.post(`/employees/${employeeId}/onboarding/${taskId}/complete`, {
      notes,
    });
  },
};
