import { apiClient } from './apiClient';

export interface AgentStepResponse {
  id: string;
  agentName: string;
  stepOrder: number;
  status: string;
  input?: string;
  output?: string;
  startedAt?: string;
  completedAt?: string;
  errorDetails?: string;
  toolCalls: ToolCallResponse[];
}

export interface ToolCallResponse {
  id: string;
  toolName: string;
  input?: string;
  output?: string;
  validated: boolean;
  durationMs: number;
}

export interface WorkflowApprovalResponse {
  id: string;
  requestedAction: string;
  requestedAt: string;
  decision?: string;
  decidedById?: string;
  decidedAt?: string;
  comments?: string;
}

export interface ValidationResultResponse {
  id: string;
  validationType: string;
  passed: boolean;
  errors?: string;
  warnings?: string;
}

export interface WorkflowExecutionResponse {
  id: string;
  objective: string;
  status: string;
  plan?: string;
  finalResult?: string;
  errorDetails?: string;
  initiatedById: string;
  companyId?: string;
  createdAt: string;
  completedAt?: string;
  agentSteps: AgentStepResponse[];
  approvals: WorkflowApprovalResponse[];
  validationResults: ValidationResultResponse[];
}

export const workflowsApi = {
  startScreening: async (applicationId: string, jobId: string) => {
    const response = await apiClient.post<WorkflowExecutionResponse>('/api/workflows/recruitment-screening', { applicationId, jobId });
    return response.data;
  },
  
  getById: async (id: string) => {
    const response = await apiClient.get<WorkflowExecutionResponse>(`/api/workflows/${id}`);
    return response.data;
  },
  
  getAll: async () => {
    const response = await apiClient.get<WorkflowExecutionResponse[]>('/api/workflows');
    return response.data;
  },
  
  approve: async (id: string, comments?: string) => {
    const response = await apiClient.post<WorkflowExecutionResponse>(`/api/workflows/${id}/approve`, { comments });
    return response.data;
  },
  
  reject: async (id: string, comments?: string) => {
    const response = await apiClient.post<WorkflowExecutionResponse>(`/api/workflows/${id}/reject`, { comments });
    return response.data;
  },
  
  requestRevision: async (id: string, comments: string) => {
    const response = await apiClient.post<WorkflowExecutionResponse>(`/api/workflows/${id}/revise`, { comments });
    return response.data;
  }
};
