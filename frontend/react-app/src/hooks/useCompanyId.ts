import { useEffect, useState } from 'react';
import apiClient from '../api/apiClient';
import { useAuthStore } from '../store/authStore';

export function useCompanyId() {
  const user = useAuthStore((state) => state.user);
  const [companyId, setCompanyId] = useState<string | undefined>(user?.companyId);

  useEffect(() => {
    if (user?.companyId) {
      setCompanyId(user.companyId);
      return;
    }
    if (!user) {
      setCompanyId(undefined);
      return;
    }
    let active = true;
    apiClient.get<{ companyId?: string }>('/Auth/me')
      .then(({ data }) => { if (active) setCompanyId(data.companyId); })
      .catch(() => { if (active) setCompanyId(undefined); });
    return () => { active = false; };
  }, [user?.id, user?.companyId]);

  return companyId;
}
