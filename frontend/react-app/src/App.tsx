import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import DashboardLayout from './components/layouts/DashboardLayout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import NotFoundPage from './pages/NotFoundPage';
import CompaniesPage from './pages/CompaniesPage';
import CompanyDetailsPage from './pages/CompanyDetailsPage';
import JobsPage from './pages/JobsPage';
import JobDetailsPage from './pages/JobDetailsPage';
import ApplicationsPage from './pages/ApplicationsPage';
import CandidateProfilePage from './pages/CandidateProfilePage';
import InterviewsPage from './pages/InterviewsPage';

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
        {/* Member 1: Company & Job routes */}
        <Route path="companies" element={<CompaniesPage />} />
        <Route path="companies/:id" element={<CompanyDetailsPage />} />
        <Route path="jobs" element={<JobsPage />} />
        <Route path="jobs/:id" element={<JobDetailsPage />} />
        {/* Member 2: Applications & Candidate Profile routes */}
        <Route path="applications" element={<ApplicationsPage />} />
        <Route path="profile" element={<CandidateProfilePage />} />
        {/* Member 3: Interviews routes */}
        <Route path="interviews" element={<InterviewsPage />} />
        <Route path="offers" element={<div>Offers — Coming Soon</div>} />
        {/* Member 4: Employees routes */}
        <Route path="employees" element={<div>Employees — Coming Soon</div>} />
        <Route path="onboarding" element={<div>Onboarding — Coming Soon</div>} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
