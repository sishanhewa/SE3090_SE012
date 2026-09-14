import apiClient from './apiClient';

export interface InterviewResponse {
  id: string;
  applicationId: string;
  interviewerId: string;
  interviewDate: string;
  durationMinutes: number;
  interviewType: string;
  meetingLink?: string;
  status: string;
  feedback?: string;
  rating?: number;
}

export interface ScheduleInterviewRequest {
  applicationId: string;
  interviewerId: string;
  interviewDate: string;
  durationMinutes: number;
  interviewType: string;
  meetingLink?: string;
}

export interface ProvideFeedbackRequest {
  feedback: string;
  rating: number;
}

export const interviewsApi = {
  getAll: async () => {
    const response = await apiClient.get<InterviewResponse[]>('/interviews');
    return response.data;
  },
  getById: async (id: string) => {
    const response = await apiClient.get<InterviewResponse>(`/interviews/${id}`);
    return response.data;
  },
  schedule: async (data: ScheduleInterviewRequest) => {
    const response = await apiClient.post<InterviewResponse>('/interviews', data);
    return response.data;
  },
  updateStatus: async (id: string, status: string) => {
    const response = await apiClient.put(`/interviews/${id}/status`, { status });
    return response.data;
  },
  provideFeedback: async (id: string, data: ProvideFeedbackRequest) => {
    const response = await apiClient.post(`/interviews/${id}/feedback`, data);
    return response.data;
  }
};
