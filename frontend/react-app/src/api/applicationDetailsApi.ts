import { useState, useEffect } from 'react';
import apiClient from './apiClient';

export interface ApplicationHistoryItem {
  id: string;
  fromStatus: string;
  toStatus: string;
  changedBy?: string;
  notes?: string;
  changedAt: string;
}

export interface DocumentItem {
  id: string;
  fileName: string;
  fileUrl: string;
  fileType: string;
  fileSizeBytes: number;
  createdAt: string;
}

export const applicationDetailsApi = {
  getHistory: async (applicationId: string): Promise<ApplicationHistoryItem[]> => {
    const response = await apiClient.get<ApplicationHistoryItem[]>(`/applications/${applicationId}/history`);
    return response.data;
  },
  getDocuments: async (applicationId: string): Promise<DocumentItem[]> => {
    const response = await apiClient.get<DocumentItem[]>(`/applications/${applicationId}/documents`);
    return response.data;
  },
  uploadDocument: async (applicationId: string, file: File): Promise<DocumentItem> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post<DocumentItem>(`/applications/${applicationId}/documents`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
  downloadDocument: async (applicationId: string, documentId: string): Promise<Blob> => {
    const response = await apiClient.get<Blob>(`/applications/${applicationId}/documents/${documentId}`, {
      responseType: 'blob',
    });
    return response.data;
  },
};

/** React hook for application history */
export function useApplicationHistory(applicationId: string | undefined) {
  const [history, setHistory] = useState<ApplicationHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!applicationId) return;
    setLoading(true);
    applicationDetailsApi
      .getHistory(applicationId)
      .then(setHistory)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [applicationId]);

  return { history, loading };
}

/** React hook for application documents */
export function useApplicationDocuments(applicationId: string | undefined) {
  const [documents, setDocuments] = useState<DocumentItem[]>([]);
  const [loading, setLoading] = useState(true);

  const refresh = () => {
    if (!applicationId) return;
    setLoading(true);
    applicationDetailsApi
      .getDocuments(applicationId)
      .then(setDocuments)
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    refresh();
  }, [applicationId]);

  return { documents, loading, refresh };
}
