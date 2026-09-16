import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';

interface RoleGuardProps {
  allowedRoles: string[];
  children: React.ReactNode;
}

/**
 * Prevents users without the required role from accessing a route.
 * If the user's roles don't intersect with allowedRoles, they are
 * redirected back to the dashboard.
 */
export default function RoleGuard({ allowedRoles, children }: RoleGuardProps) {
  const { user } = useAuthStore();
  const userRoles = user?.roles ?? [];

  const hasAccess = allowedRoles.length === 0 || allowedRoles.some((r) => userRoles.includes(r));

  if (!hasAccess) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
