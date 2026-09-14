import apiClient from './apiClient';

export interface CandidateProfileResponse {
  id: string;
  userId: string;
  skills: string[];
  experienceYears: number;
  educationLevel: string;
  portfolioUrl?: string;
  resumeUrl?: string;
  updatedAt?: string;
}

export interface UpdateCandidateProfileRequest {
  skills: string[];
  experienceYears: number;
  educationLevel: string;
  portfolioUrl?: string;
  resumeUrl?: string;
}

export const candidateProfileApi = {
  getProfile: async () => {
    const response = await apiClient.get<CandidateProfileResponse>('/candidateprofiles/me');
    return response.data;
  },
  updateProfile: async (data: UpdateCandidateProfileRequest) => {
    const response = await apiClient.put<CandidateProfileResponse>('/candidateprofiles/me', data);
    return response.data;
  }
};
