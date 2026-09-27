import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { applicationsApi, type ApplicationResponse } from '../api/applicationsApi';
import { useAuthStore } from '../store/authStore';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ArrowLeft, XCircle } from 'lucide-react';

export default function ApplicationDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuthStore();
  
  const roles = user?.roles ?? [];
  const isStaff = roles.some((r) => ['SystemAdmin', 'Recruiter', 'HiringManager'].includes(r));

  const [application, setApplication] = useState<ApplicationResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchApplication = async () => {
    if (!id) return;
    try {
      const data = await applicationsApi.getById(id);
      setApplication(data);
    } catch (error) {
      console.error('Failed to fetch application', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchApplication();
  }, [id]);

  const handleUpdateStatus = async (newStatus: string) => {
    if (!id) return;
    try {
      await applicationsApi.updateStatus(id, newStatus, `Status updated to ${newStatus}`);
      fetchApplication();
    } catch (error) {
      console.error('Failed to update status', error);
      alert('Failed to update status. Please check if the transition is allowed.');
    }
  };

  const handleWithdraw = async () => {
    if (!id) return;
    if (!window.confirm('Are you sure you want to withdraw this application?')) return;
    try {
      await applicationsApi.withdraw(id);
      fetchApplication();
    } catch (error) {
      console.error('Failed to withdraw application', error);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading application details...</div>;
  }

  if (!application) {
    return <div className="p-8 text-center text-destructive">Application not found.</div>;
  }

  return (
    <div className="p-8 space-y-6 max-w-4xl mx-auto">
      <Button variant="ghost" onClick={() => navigate(-1)} className="mb-4 gap-2">
        <ArrowLeft className="h-4 w-4" /> Back to Applications
      </Button>

      <div className="flex justify-between items-start">
        <div>
          <h2 className="text-3xl font-bold tracking-tight mb-2">Application for {application.jobTitle}</h2>
          <p className="text-muted-foreground">{application.companyName}</p>
        </div>
        <Badge className="text-lg py-1 px-4" variant={
          application.status === 'Submitted' ? 'default' : 
          application.status === 'Withdrawn' ? 'destructive' : 
          application.status === 'Hired' ? 'default' : 
          application.status === 'Rejected' ? 'destructive' : 'secondary'
        }>
          {application.status}
        </Badge>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Application Info</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm text-muted-foreground">Applied On</label>
              <div className="font-medium">{new Date(application.submittedAt).toLocaleString()}</div>
            </div>
            {isStaff && (
              <div>
                <label className="text-sm text-muted-foreground">Candidate Name</label>
                <div className="font-medium">{application.candidateName}</div>
              </div>
            )}
            {application.aiScore && (
              <div>
                <label className="text-sm text-muted-foreground">AI Match Score</label>
                <div className="font-medium">{application.aiScore}%</div>
              </div>
            )}
            {application.aiRecommendation && (
              <div>
                <label className="text-sm text-muted-foreground">AI Recommendation</label>
                <div className="font-medium">{application.aiRecommendation}</div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Cover Letter</CardTitle>
          </CardHeader>
          <CardContent>
            {application.coverLetter ? (
              <p className="whitespace-pre-wrap">{application.coverLetter}</p>
            ) : (
              <p className="text-muted-foreground italic">No cover letter provided.</p>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="flex gap-4 pt-4 border-t">
        {isStaff && application.status !== 'Hired' && application.status !== 'Rejected' && application.status !== 'Withdrawn' && (
          <>
            {application.status === 'Submitted' && <Button onClick={() => handleUpdateStatus('Screening')}>Move to Screening</Button>}
            {application.status === 'Screening' && <Button onClick={() => handleUpdateStatus('Shortlisted')}>Shortlist</Button>}
            {application.status === 'Shortlisted' && <Button onClick={() => handleUpdateStatus('Interview')}>Invite to Interview</Button>}
            {application.status === 'Interview' && <Button onClick={() => handleUpdateStatus('Offered')}>Extend Offer</Button>}
            {application.status === 'Offered' && <Button onClick={() => handleUpdateStatus('Hired')}>Mark as Hired</Button>}
            <Button variant="destructive" onClick={() => handleUpdateStatus('Rejected')}>Reject</Button>
          </>
        )}
        
        {!isStaff && application.status !== 'Withdrawn' && application.status !== 'Rejected' && application.status !== 'Hired' && (
          <Button variant="destructive" onClick={handleWithdraw} className="gap-2">
            <XCircle className="h-4 w-4" /> Withdraw Application
          </Button>
        )}
      </div>
    </div>
  );
}
