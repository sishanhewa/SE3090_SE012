import { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { applicationsApi, type ApplicationResponse } from '../api/applicationsApi';
import { useAuthStore } from '../store/authStore';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { FileText, XCircle } from 'lucide-react';

export default function ApplicationsPage() {
  const { user } = useAuthStore();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const jobId = searchParams.get('jobId');
  
  const roles = user?.roles ?? [];
  const isStaff = roles.some((r) => ['SystemAdmin', 'Recruiter', 'HiringManager'].includes(r));

  const [applications, setApplications] = useState<ApplicationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<string>('All');

  const fetchApplications = async () => {
    setLoading(true);
    try {
      const data = isStaff 
        ? await applicationsApi.getAll(jobId || undefined)
        : await applicationsApi.getMine();
      setApplications(data);
    } catch (error) {
      console.error('Failed to fetch applications', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchApplications();
  }, [jobId]);

  const handleWithdraw = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation();
    if (!window.confirm('Are you sure you want to withdraw this application?')) return;
    try {
      await applicationsApi.withdraw(id);
      fetchApplications();
    } catch (error) {
      console.error('Failed to withdraw application', error);
    }
  };

  const filteredApps = activeTab === 'All' 
    ? applications 
    : applications.filter(a => a.status === activeTab);

  const tabs = ['All', 'Submitted', 'Screening', 'Shortlisted', 'Interview', 'Offered', 'Hired', 'Rejected', 'Withdrawn'];

  return (
    <div className="p-8 space-y-6">
      <div>
        <h2 className="text-3xl font-bold tracking-tight">Applications</h2>
        <p className="text-muted-foreground">
          {isStaff
            ? (jobId ? 'Viewing applications for selected job.' : 'Review and manage incoming candidate applications.')
            : 'Track and manage your job applications.'}
        </p>
      </div>

      <div className="flex gap-2 overflow-x-auto pb-2">
        {tabs.map(tab => (
          <Button 
            key={tab} 
            variant={activeTab === tab ? 'default' : 'outline'}
            size="sm"
            onClick={() => setActiveTab(tab)}
          >
            {tab}
          </Button>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-primary" />
            {isStaff ? 'All Applications' : 'My Applications'}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex justify-center py-8 text-muted-foreground">Loading applications...</div>
          ) : filteredApps.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground border-2 border-dashed rounded-lg">
              <FileText className="h-12 w-12 mb-4 opacity-50" />
              <p>No applications found for this filter.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Job Title</TableHead>
                  {isStaff && <TableHead>Candidate</TableHead>}
                  <TableHead>Applied Date</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredApps.map((app) => (
                  <TableRow 
                    key={app.id} 
                    className="cursor-pointer hover:bg-muted/50 transition-colors"
                    onClick={() => navigate(`/applications/${app.id}`)}
                  >
                    <TableCell className="font-medium">
                      {app.jobTitle}
                      <div className="text-xs text-muted-foreground font-normal">{app.companyName}</div>
                    </TableCell>
                    {isStaff && <TableCell>{app.candidateName}</TableCell>}
                    <TableCell>
                      {new Date(app.submittedAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <Badge variant={
                        app.status === 'Submitted' ? 'default' : 
                        app.status === 'Withdrawn' ? 'destructive' : 
                        app.status === 'Hired' ? 'default' : 
                        app.status === 'Rejected' ? 'destructive' : 'secondary'
                      }>
                        {app.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      {!isStaff && app.status !== 'Withdrawn' && app.status !== 'Rejected' && app.status !== 'Hired' && (
                        <Button 
                          variant="destructive" 
                          size="sm" 
                          onClick={(e) => handleWithdraw(e, app.id)}
                          className="gap-2"
                        >
                          <XCircle className="h-4 w-4" /> Withdraw
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

