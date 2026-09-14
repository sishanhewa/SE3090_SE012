import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import {
  LayoutDashboard,
  Building2,
  Briefcase,
  FileText,
  CalendarDays,
  Mail,
  Users,
  Rocket,
  LogOut,
} from 'lucide-react';

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/companies', label: 'Companies', icon: Building2 },
  { to: '/jobs', label: 'Jobs', icon: Briefcase },
  { to: '/applications', label: 'Applications', icon: FileText },
  { to: '/interviews', label: 'Interviews', icon: CalendarDays },
  { to: '/offers', label: 'Offers', icon: Mail },
  { to: '/employees', label: 'Employees', icon: Users },
  { to: '/onboarding', label: 'Onboarding', icon: Rocket },
];

export default function DashboardLayout() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="flex h-screen bg-muted/20">
      {/* Sidebar */}
      <aside className="w-64 bg-card border-r flex flex-col justify-between">
        <div>
          <div className="h-16 flex items-center px-6 border-b">
            <h1 className="text-xl font-bold text-primary flex items-center gap-2">
              <Rocket className="h-6 w-6 text-primary" />
              TalentFlow
              <span className="text-xs font-semibold bg-primary/10 text-primary px-2 py-0.5 rounded-full">AI</span>
            </h1>
          </div>

          <nav className="p-4 space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                      isActive
                        ? 'bg-primary/10 text-primary'
                        : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                    }`
                  }
                >
                  <Icon className="h-5 w-5" />
                  {item.label}
                </NavLink>
              );
            })}
          </nav>
        </div>

        <div className="p-4 border-t">
          <div className="flex items-center gap-3 mb-4 px-2">
            <div className="h-9 w-9 rounded-full bg-primary/10 flex items-center justify-center text-primary font-semibold">
              {user?.firstName?.[0] || 'U'}
              {user?.lastName?.[0] || ''}
            </div>
            <div className="flex flex-col">
              <span className="text-sm font-medium text-foreground">
                {user?.firstName || 'User'} {user?.lastName || ''}
              </span>
              <span className="text-xs text-muted-foreground">{user?.roles?.[0] ?? 'User'}</span>
            </div>
          </div>
          <button
            onClick={handleLogout}
            className="w-full flex items-center justify-center gap-2 px-3 py-2 text-sm font-medium text-destructive hover:bg-destructive/10 rounded-md transition-colors"
          >
            <LogOut className="h-4 w-4" />
            Logout
          </button>
        </div>
      </aside>

      {/* Main content area */}
      <main className="flex-1 overflow-auto bg-background/50">
        <Outlet />
      </main>
    </div>
  );
}
