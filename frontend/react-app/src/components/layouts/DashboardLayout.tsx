import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';

const navItems = [
  { to: '/', label: 'Dashboard', icon: '📊' },
  { to: '/companies', label: 'Companies', icon: '🏢' },
  { to: '/jobs', label: 'Jobs', icon: '💼' },
  { to: '/applications', label: 'Applications', icon: '📄' },
  { to: '/interviews', label: 'Interviews', icon: '🗓️' },
  { to: '/offers', label: 'Offers', icon: '📨' },
  { to: '/employees', label: 'Employees', icon: '👥' },
  { to: '/onboarding', label: 'Onboarding', icon: '🚀' },
];

export default function DashboardLayout() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="dashboard-layout">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="sidebar-header">
          <h1 className="sidebar-logo">TalentFlow</h1>
          <span className="sidebar-badge">AI</span>
        </div>

        <nav className="sidebar-nav">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                `nav-item ${isActive ? 'nav-item--active' : ''}`
              }
            >
              <span className="nav-icon">{item.icon}</span>
              <span className="nav-label">{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="user-info">
            <div className="user-avatar">
              {user?.firstName?.[0]}
              {user?.lastName?.[0]}
            </div>
            <div className="user-details">
              <span className="user-name">
                {user?.firstName} {user?.lastName}
              </span>
              <span className="user-role">{user?.roles?.[0] ?? 'User'}</span>
            </div>
          </div>
          <button className="logout-btn" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </aside>

      {/* Main content area */}
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
