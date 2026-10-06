export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

export interface ChatRequest {
  messages: ChatMessage[];
  companyId?: string; // Optional, backend might enforce it from token
}

export interface ChatResponse {
  response: string;
}
