import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { interviewsApi, type InterviewResponse, type ScheduleInterviewRequest } from '../api/interviewsApi';
import { applicationsApi, type ApplicationResponse } from '../api/applicationsApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { CalendarDays, Plus, Video, CheckCircle, XCircle } from 'lucide-react';

export default function InterviewsPage() {
  const navigate = useNavigate();
  const [interviews, setInterviews] = useState<InterviewResponse[]>([]);
  const [applications, setApplications] = useState<ApplicationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  
  const [formData, setFormData] = useState<ScheduleInterviewRequest>({
    applicationId: '',
    scheduledAt: '',
    durationMinutes: 60,
    meetingUrl: '',
    location: '',
    notes: ''
  });

  const fetchData = async () => {
    try {
      const [interviewsData, applicationsData] = await Promise.all([
        interviewsApi.getAll(),
        applicationsApi.getAll()
      ]);
      setInterviews(interviewsData);
      setApplications(applicationsData);
    } catch (error) {
      console.error('Failed to fetch data', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await interviewsApi.schedule({
        ...formData,
        scheduledAt: new Date(formData.scheduledAt).toISOString()
      });
      setIsModalOpen(false);
      setFormData({ 
        applicationId: '', scheduledAt: '', durationMinutes: 60, meetingUrl: '', location: '', notes: '' 
      });
      fetchData();
    } catch (error) {
      console.error('Failed to schedule interview', error);
      alert('Failed to schedule interview.');
    }
  };

  const handleUpdateStatus = async (e: React.MouseEvent, id: string, status: string) => {
    e.stopPropagation();
    try {
      await interviewsApi.updateStatus(id, status);
      fetchData();
    } catch (error) {
      console.error('Failed to update status', error);
    }
  };

  const handleCancel = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation();
    if (!window.confirm('Cancel this interview?')) return;
    try {
      await interviewsApi.cancel(id);
      fetchData();
    } catch (error) {
      console.error('Failed to cancel', error);
    }
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Interviews</h2>
          <p className="text-muted-foreground">Manage and schedule candidate interviews.</p>
        </div>
        
        <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
          <DialogTrigger asChild>
            <Button className="gap-2">
              <Plus className="h-4 w-4" />
              Schedule Interview
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Schedule a New Interview</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-4">
              <select 
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.applicationId}
                onChange={(e) => setFormData({ ...formData, applicationId: e.target.value })}
                required
              >
                <option value="" disabled>Select Application</option>
                {applications.filter(a => a.status === 'Shortlisted' || a.status === 'Interview').map(app => (
                  <option key={app.id} value={app.id}>
                    {app.jobTitle} - {app.candidateName} ({app.status})
                  </option>
                ))}
              </select>
              
              <Input
                type="datetime-local"
                value={formData.scheduledAt}
                onChange={(e) => setFormData({ ...formData, scheduledAt: e.target.value })}
                required
              />
              
              <Input
                type="number"
                placeholder="Duration (mins)"
                value={formData.durationMinutes}
                onChange={(e) => setFormData({ ...formData, durationMinutes: Number(e.target.value) })}
                required
              />

              <Input
                placeholder="Meeting Link (Optional)"
                type="url"
                value={formData.meetingUrl}
                onChange={(e) => setFormData({ ...formData, meetingUrl: e.target.value })}
              />

              <Input
                placeholder="Location (Optional)"
                type="text"
                value={formData.location}
                onChange={(e) => setFormData({ ...formData, location: e.target.value })}
              />
              
              <Button type="submit" className="w-full">Schedule Interview</Button>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CalendarDays className="h-5 w-5 text-primary" />
            Upcoming & Past Interviews
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex justify-center py-8 text-muted-foreground">Loading interviews...</div>
          ) : interviews.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground border-2 border-dashed rounded-lg">
              <CalendarDays className="h-12 w-12 mb-4 opacity-50" />
              <p>No interviews scheduled yet.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Date & Time</TableHead>
                  <TableHead>Duration</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {interviews.map((interview) => (
                  <TableRow 
                    key={interview.id}
                    className="cursor-pointer hover:bg-muted/50 transition-colors"
                    onClick={() => navigate(`/interviews/${interview.id}`)}
                  >
                    <TableCell className="font-medium">
                      {new Date(interview.scheduledAt).toLocaleString()}
                    </TableCell>
                    <TableCell>{interview.durationMinutes} mins</TableCell>
                    <TableCell>
                      <Badge variant={
                        interview.status === 'Completed' ? 'default' : 
                        interview.status === 'Cancelled' ? 'destructive' : 'secondary'
                      }>
                        {interview.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right space-x-2">
                      {interview.meetingUrl && (
                        <Button variant="outline" size="sm" onClick={e => e.stopPropagation()} asChild>
                          <a href={interview.meetingUrl} target="_blank" rel="noreferrer">
                            <Video className="h-4 w-4 mr-2" /> Join
                          </a>
                        </Button>
                      )}
                      {interview.status === 'Proposed' && (
                        <>
                          <Button variant="ghost" size="sm" onClick={(e) => handleUpdateStatus(e, interview.id, 'Scheduled')}>
                            <CheckCircle className="h-4 w-4 text-blue-500" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={(e) => handleCancel(e, interview.id)}>
                            <XCircle className="h-4 w-4 text-red-500" />
                          </Button>
                        </>
                      )}
                      {interview.status === 'Scheduled' && (
                        <>
                          <Button variant="ghost" size="sm" onClick={(e) => handleUpdateStatus(e, interview.id, 'Completed')}>
                            <CheckCircle className="h-4 w-4 text-green-500" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={(e) => handleCancel(e, interview.id)}>
                            <XCircle className="h-4 w-4 text-red-500" />
                          </Button>
                        </>
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
