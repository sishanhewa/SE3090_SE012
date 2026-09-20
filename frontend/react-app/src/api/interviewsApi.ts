import apiClient from './apiClient';

export interface InterviewFeedbackResponse {
  id: string;
  reviewerName: string;
  overallScore: number;
  recommendation: string;
  comments?: string;
  createdAt: string;
}

export interface InterviewResponse {
  id: string;
  applicationId: string;
  scheduledAt: string;
  durationMinutes: number;
  status: string;
  meetingUrl?: string;
  location?: string;
  notes?: string;
  feedbacks?: InterviewFeedbackResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface ScheduleInterviewRequest {
  applicationId: string;
  scheduledAt: string;
  durationMinutes: number;
  meetingUrl?: string;
  location?: string;
  notes?: string;
}

export interface AddInterviewFeedbackRequest {
  technicalScore: number;
  communicationScore: number;
  experienceScore: number;
  comments?: string;
  recommendation: string;
}

export const interviewsApi = {
  getAll: async () => {
    const response = await apiClient.get<PagedResult<InterviewResponse>>('/interviews');
    return response.data.items;
  },
  getByApplication: async (applicationId: string) => {
    const response = await apiClient.get<PagedResult<InterviewResponse>>(`/interviews/application/${applicationId}`);
    return response.data.items;
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
    const response = await apiClient.patch(`/interviews/${id}/status`, { status });
    return response.data;
  },
  provideFeedback: async (id: string, data: AddInterviewFeedbackRequest) => {
    const response = await apiClient.post(`/interviews/${id}/feedback`, data);
    return response.data;
  },
  cancel: async (id: string) => {
    const response = await apiClient.post(`/interviews/${id}/cancel`);
    return response.data;
  }
};
