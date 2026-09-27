import { useState, useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { Briefcase, FileText, CalendarDays, Users, UserCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import { jobsApi } from '../api/jobsApi';
import { applicationsApi } from '../api/applicationsApi';
import { interviewsApi } from '../api/interviewsApi';
import { employeesApi } from '../api/employeesApi';

const staffCards = [
  { label: 'Active Jobs', icon: Briefcase, color: 'text-blue-600 bg-blue-50', to: '/jobs' },
  { label: 'Applications', icon: FileText, color: 'text-green-600 bg-green-50', to: '/applications' },
  { label: 'Interviews', icon: CalendarDays, color: 'text-purple-600 bg-purple-50', to: '/interviews' },
  { label: 'Employees', icon: Users, color: 'text-amber-600 bg-amber-50', to: '/employees' },
];

const candidateCards = [
  { label: 'My Applications', icon: FileText, color: 'text-green-600 bg-green-50', to: '/applications' },
  { label: 'My Profile', icon: UserCircle, color: 'text-blue-600 bg-blue-50', to: '/profile' },
];

export default function DashboardPage() {
  const { user } = useAuthStore();
  const roles = user?.roles ?? [];
  const isStaff = roles.some((r) => ['SystemAdmin', 'Recruiter', 'HiringManager'].includes(r));
  const isCandidate = roles.includes('Candidate');

  const [stats, setStats] = useState({
    jobs: 0,
    applications: 0,
    interviews: 0,
    employees: 0,
  });

  useEffect(() => {
    const fetchStats = async () => {
      try {
        if (isStaff) {
          const [jobsData, appsData, interviewsData, employeesData] = await Promise.all([
            jobsApi.getAll(),
            applicationsApi.getAll(),
            interviewsApi.getAll(),
            employeesApi.getAll('company-1')
          ]);
          setStats({
            jobs: jobsData.length,
            applications: appsData.length,
            interviews: interviewsData.length,
            employees: employeesData.items ? employeesData.items.length : 0,
          });
        } else if (isCandidate) {
          const appsData = await applicationsApi.getMine();
          setStats({
            ...stats,
            applications: appsData.length,
          });
        }
      } catch (error) {
        console.error('Failed to fetch dashboard stats', error);
      }
    };
    fetchStats();
  }, [isStaff, isCandidate]);

  const cards = isStaff ? staffCards : isCandidate ? candidateCards : [];

  const greeting = isStaff
    ? 'Here is your recruitment overview'
    : isCandidate
    ? 'Track your applications and profile'
    : 'Welcome to TalentFlow';

  return (
    <div className="p-8 space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          Welcome back, {user?.firstName}!
        </h1>
        <p className="text-muted-foreground mt-1">{greeting}</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {cards.map((card) => {
          const Icon = card.icon;
          
          let displayCount = '—';
          if (card.label.includes('Jobs')) displayCount = stats.jobs.toString();
          if (card.label.includes('Applications')) displayCount = stats.applications.toString();
          if (card.label.includes('Interviews')) displayCount = stats.interviews.toString();
          if (card.label.includes('Employees')) displayCount = stats.employees.toString();

          return (
            <Link
              key={card.label}
              to={card.to}
              className="group flex items-center gap-4 p-6 rounded-2xl border bg-card hover:shadow-md transition-all duration-200"
            >
              <div className={`h-12 w-12 rounded-xl flex items-center justify-center ${card.color}`}>
                <Icon className="h-6 w-6" />
              </div>
              <div>
                <p className="text-2xl font-bold text-foreground">{displayCount}</p>
                <p className="text-sm text-muted-foreground">{card.label}</p>
              </div>
            </Link>
          );
        })}
      </div>

      {isStaff && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="rounded-2xl border bg-card p-6">
            <h3 className="text-lg font-semibold mb-4">Recent Activity</h3>
            <p className="text-sm text-muted-foreground">No recent activity to display.</p>
          </div>
          <div className="rounded-2xl border bg-card p-6">
            <h3 className="text-lg font-semibold mb-4">Quick Actions</h3>
            <div className="space-y-2">
              <Link to="/jobs" className="block p-3 rounded-xl hover:bg-muted transition-colors text-sm font-medium">
                📝 Post a new job
              </Link>
              <Link to="/applications" className="block p-3 rounded-xl hover:bg-muted transition-colors text-sm font-medium">
                📄 Review applications
              </Link>
              <Link to="/interviews" className="block p-3 rounded-xl hover:bg-muted transition-colors text-sm font-medium">
                🗓️ Schedule interviews
              </Link>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
