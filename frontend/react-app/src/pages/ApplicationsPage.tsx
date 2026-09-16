import { useState, useEffect } from 'react';
import { applicationsApi, type ApplicationResponse } from '../api/applicationsApi';
import { useAuthStore } from '../store/authStore';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { FileText, XCircle } from 'lucide-react';

export default function ApplicationsPage() {
  const { user } = useAuthStore();
  const roles = user?.roles ?? [];
  const isStaff = roles.some((r) => ['SystemAdmin', 'Recruiter', 'HiringManager'].includes(r));

  const [applications, setApplications] = useState<ApplicationResponse[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchApplications = async () => {
    try {
      const data = await applicationsApi.getAll();
      setApplications(data);
    } catch (error) {
      console.error('Failed to fetch applications', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchApplications();
  }, []);

  const handleWithdraw = async (id: string) => {
    if (!window.confirm('Are you sure you want to withdraw this application?')) return;
    try {
      await applicationsApi.withdraw(id);
      fetchApplications();
    } catch (error) {
      console.error('Failed to withdraw application', error);
    }
  };

  return (
    <div className="p-8 space-y-6">
      <div>
        <h2 className="text-3xl font-bold tracking-tight">Applications</h2>
        <p className="text-muted-foreground">
          {isStaff
            ? 'Review and manage incoming candidate applications.'
            : 'Track and manage your job applications.'}
        </p>
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
          ) : applications.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground border-2 border-dashed rounded-lg">
              <FileText className="h-12 w-12 mb-4 opacity-50" />
              <p>{isStaff ? 'No applications received yet.' : "You haven't submitted any applications yet."}</p>
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
                {applications.map((app) => (
                  <TableRow key={app.id}>
                    <TableCell className="font-medium">
                      {app.job ? app.job.title : 'Unknown Job'}
                    </TableCell>
                    {isStaff && <TableCell>{app.candidateId?.slice(0, 8) ?? '—'}</TableCell>}
                    <TableCell>
                      {new Date(app.appliedDate).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <Badge variant={
                        app.status === 'Submitted' ? 'default' : 
                        app.status === 'Withdrawn' ? 'destructive' : 
                        app.status === 'Hired' ? 'default' : 'secondary'
                      }>
                        {app.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      {!isStaff && app.status !== 'Withdrawn' && app.status !== 'Rejected' && (
                        <Button 
                          variant="destructive" 
                          size="sm" 
                          onClick={() => handleWithdraw(app.id)}
                          className="gap-2"
                        >
                          <XCircle className="h-4 w-4" /> Withdraw
                        </Button>
                      )}
                      {isStaff && (
                        <Button variant="outline" size="sm">
                          View Details
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

