import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import RoleGuard from './components/auth/RoleGuard';
import DashboardLayout from './components/layouts/DashboardLayout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import NotFoundPage from './pages/NotFoundPage';
import CompaniesPage from './pages/CompaniesPage';
import CompanyDetailsPage from './pages/CompanyDetailsPage';
import JobsPage from './pages/JobsPage';
import JobDetailsPage from './pages/JobDetailsPage';
import EditJobPage from './pages/EditJobPage';
import ApplicationsPage from './pages/ApplicationsPage';
import ApplicationDetailsPage from './pages/ApplicationDetailsPage';
import CandidateProfilePage from './pages/CandidateProfilePage';
import InterviewsPage from './pages/InterviewsPage';
import InterviewDetailsPage from './pages/InterviewDetailsPage';
import OffersPage from './pages/OffersPage';
import EmployeesPage from './pages/EmployeesPage';
import EmployeeDetailsPage from './pages/EmployeeDetailsPage';
import OnboardingPage from './pages/OnboardingPage';

const STAFF = ['SystemAdmin', 'Recruiter', 'HiringManager'];
const ALL_ROLES = ['SystemAdmin', 'Recruiter', 'HiringManager', 'Candidate', 'Employee'];

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore();
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      {/* Protected routes wrapped in dashboard layout */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />

        {/* Companies — Staff only */}
        <Route path="companies" element={<RoleGuard allowedRoles={STAFF}><CompaniesPage /></RoleGuard>} />
        <Route path="companies/:id" element={<RoleGuard allowedRoles={STAFF}><CompanyDetailsPage /></RoleGuard>} />

        {/* Jobs — Staff only */}
        <Route path="jobs" element={<RoleGuard allowedRoles={STAFF}><JobsPage /></RoleGuard>} />
        <Route path="jobs/:id" element={<RoleGuard allowedRoles={STAFF}><JobDetailsPage /></RoleGuard>} />
        <Route path="jobs/:id/edit" element={<RoleGuard allowedRoles={STAFF}><EditJobPage /></RoleGuard>} />

        {/* Applications — Staff + Candidates */}
        <Route path="applications" element={<RoleGuard allowedRoles={[...STAFF, 'Candidate']}><ApplicationsPage /></RoleGuard>} />
        <Route path="applications/:id" element={<RoleGuard allowedRoles={[...STAFF, 'Candidate']}><ApplicationDetailsPage /></RoleGuard>} />

        {/* Profile — Candidates only */}
        <Route path="profile" element={<RoleGuard allowedRoles={['Candidate']}><CandidateProfilePage /></RoleGuard>} />

        {/* Interviews */}
        <Route path="interviews" element={<RoleGuard allowedRoles={STAFF}><InterviewsPage /></RoleGuard>} />
        <Route path="interviews/:id" element={<RoleGuard allowedRoles={STAFF}><InterviewDetailsPage /></RoleGuard>} />

        {/* Offers — Staff only */}
        <Route path="offers" element={<RoleGuard allowedRoles={STAFF}><OffersPage /></RoleGuard>} />

        {/* Employees & Onboarding — Staff only */}
        <Route path="onboarding" element={<RoleGuard allowedRoles={STAFF}><OnboardingPage /></RoleGuard>} />
        <Route path="employees" element={<RoleGuard allowedRoles={STAFF}><EmployeesPage /></RoleGuard>} />
        <Route path="employees/:id" element={<RoleGuard allowedRoles={STAFF}><EmployeeDetailsPage /></RoleGuard>} />

        {/* Onboarding — Staff only */}
        <Route path="onboarding" element={<RoleGuard allowedRoles={STAFF}><div className="p-8"><h2 className="text-3xl font-bold">Onboarding</h2><p className="text-muted-foreground mt-2">Coming in Sprint 2 — Phase 5</p></div></RoleGuard>} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

