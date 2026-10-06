import apiClient from './apiClient';
import type { ChatRequest, ChatResponse } from '../types/chat';

export const chatApi = {
  sendMessage: async (request: ChatRequest): Promise<ChatResponse> => {
    const response = await apiClient.post<ChatResponse>('/chat', request);
    return response.data;
  },
};
