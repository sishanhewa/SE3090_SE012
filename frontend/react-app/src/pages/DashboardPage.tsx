import { useAuthStore } from '../store/authStore';

export default function DashboardPage() {
  const { user } = useAuthStore();

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1>Welcome back, {user?.firstName}!</h1>
        <p className="page-subtitle">Here is your recruitment overview</p>
      </div>

      <div className="stats-grid">
        <div className="stat-card stat-card--blue">
          <div className="stat-icon">💼</div>
          <div className="stat-content">
            <span className="stat-value">—</span>
            <span className="stat-label">Active Jobs</span>
          </div>
        </div>

        <div className="stat-card stat-card--green">
          <div className="stat-icon">📄</div>
          <div className="stat-content">
            <span className="stat-value">—</span>
            <span className="stat-label">Applications</span>
          </div>
        </div>

        <div className="stat-card stat-card--purple">
          <div className="stat-icon">🗓️</div>
          <div className="stat-content">
            <span className="stat-value">—</span>
            <span className="stat-label">Interviews</span>
          </div>
        </div>

        <div className="stat-card stat-card--orange">
          <div className="stat-icon">👥</div>
          <div className="stat-content">
            <span className="stat-value">—</span>
            <span className="stat-label">Employees</span>
          </div>
        </div>
      </div>
    </div>
  );
}
