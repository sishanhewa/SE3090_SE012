import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { interviewsApi, type InterviewResponse } from '../api/interviewsApi';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ArrowLeft, CheckCircle, XCircle } from 'lucide-react';
import { Input } from '@/components/ui/input';

export default function InterviewDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  const [interview, setInterview] = useState<InterviewResponse | null>(null);
  const [loading, setLoading] = useState(true);
  
  const [feedbackForm, setFeedbackForm] = useState({
    technicalScore: 0,
    communicationScore: 0,
    experienceScore: 0,
    comments: '',
    recommendation: 'Hire'
  });

  const fetchInterview = async () => {
    if (!id) return;
    try {
      const data = await interviewsApi.getById(id);
      setInterview(data);
    } catch (error) {
      console.error('Failed to fetch interview', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchInterview();
  }, [id]);

  const handleUpdateStatus = async (status: string) => {
    if (!id) return;
    try {
      await interviewsApi.updateStatus(id, status);
      fetchInterview();
    } catch (error) {
      console.error('Failed to update status', error);
    }
  };

  const handleCancel = async () => {
    if (!id) return;
    if (!window.confirm('Cancel this interview?')) return;
    try {
      await interviewsApi.cancel(id);
      fetchInterview();
    } catch (error) {
      console.error('Failed to cancel', error);
    }
  };

  const handleSubmitFeedback = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;
    try {
      await interviewsApi.provideFeedback(id, feedbackForm);
      alert('Feedback submitted successfully!');
      fetchInterview();
    } catch (error) {
      console.error('Failed to submit feedback', error);
      alert('Failed to submit feedback.');
    }
  };

  if (loading) return <div className="p-8 text-center">Loading interview details...</div>;
  if (!interview) return <div className="p-8 text-center text-destructive">Interview not found.</div>;

  return (
    <div className="p-8 space-y-6 max-w-4xl mx-auto">
      <Button variant="ghost" onClick={() => navigate(-1)} className="mb-4 gap-2">
        <ArrowLeft className="h-4 w-4" /> Back to Interviews
      </Button>

      <div className="flex justify-between items-start">
        <div>
          <h2 className="text-3xl font-bold tracking-tight mb-2">Interview Details</h2>
          <p className="text-muted-foreground">ID: {interview.id}</p>
        </div>
        <Badge className="text-lg py-1 px-4" variant={
          interview.status === 'Completed' ? 'default' : 
          interview.status === 'Cancelled' ? 'destructive' : 'secondary'
        }>
          {interview.status}
        </Badge>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Schedule Info</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm text-muted-foreground">Scheduled At</label>
              <div className="font-medium">{new Date(interview.scheduledAt).toLocaleString()}</div>
            </div>
            <div>
              <label className="text-sm text-muted-foreground">Duration</label>
              <div className="font-medium">{interview.durationMinutes} minutes</div>
            </div>
            {interview.meetingUrl && (
              <div>
                <label className="text-sm text-muted-foreground">Meeting Link</label>
                <div className="font-medium">
                  <a href={interview.meetingUrl} target="_blank" rel="noreferrer" className="text-blue-500 hover:underline">
                    {interview.meetingUrl}
                  </a>
                </div>
              </div>
            )}
            {interview.location && (
              <div>
                <label className="text-sm text-muted-foreground">Location</label>
                <div className="font-medium">{interview.location}</div>
              </div>
            )}
            {interview.notes && (
              <div>
                <label className="text-sm text-muted-foreground">Notes</label>
                <div className="font-medium whitespace-pre-wrap">{interview.notes}</div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Actions</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {interview.status === 'Proposed' && (
              <>
                <Button className="w-full gap-2" onClick={() => handleUpdateStatus('Scheduled')}>
                  <CheckCircle className="h-4 w-4" /> Mark as Scheduled
                </Button>
                <Button variant="destructive" className="w-full gap-2" onClick={handleCancel}>
                  <XCircle className="h-4 w-4" /> Cancel Interview
                </Button>
              </>
            )}
            {interview.status === 'Scheduled' && (
              <>
                <Button className="w-full gap-2" onClick={() => handleUpdateStatus('Completed')}>
                  <CheckCircle className="h-4 w-4" /> Mark as Completed
                </Button>
                <Button variant="destructive" className="w-full gap-2" onClick={handleCancel}>
                  <XCircle className="h-4 w-4" /> Cancel Interview
                </Button>
              </>
            )}
            {interview.status === 'Completed' && (
              <div className="text-sm text-muted-foreground text-center">
                This interview has been marked as completed.
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {(interview.status === 'Completed' || interview.status === 'Scheduled') && (
        <Card>
          <CardHeader>
            <CardTitle>Provide Feedback</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmitFeedback} className="space-y-4">
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="text-sm text-muted-foreground">Technical Score (1-10)</label>
                  <Input 
                    type="number" min="1" max="10" required
                    value={feedbackForm.technicalScore || ''} 
                    onChange={e => setFeedbackForm({...feedbackForm, technicalScore: parseInt(e.target.value) || 0})}
                  />
                </div>
                <div>
                  <label className="text-sm text-muted-foreground">Communication (1-10)</label>
                  <Input 
                    type="number" min="1" max="10" required
                    value={feedbackForm.communicationScore || ''} 
                    onChange={e => setFeedbackForm({...feedbackForm, communicationScore: parseInt(e.target.value) || 0})}
                  />
                </div>
                <div>
                  <label className="text-sm text-muted-foreground">Experience (1-10)</label>
                  <Input 
                    type="number" min="1" max="10" required
                    value={feedbackForm.experienceScore || ''} 
                    onChange={e => setFeedbackForm({...feedbackForm, experienceScore: parseInt(e.target.value) || 0})}
                  />
                </div>
              </div>

              <div>
                <label className="text-sm text-muted-foreground">Recommendation</label>
                <select 
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={feedbackForm.recommendation}
                  onChange={e => setFeedbackForm({...feedbackForm, recommendation: e.target.value})}
                  required
                >
                  <option value="StrongHire">Strong Hire</option>
                  <option value="Hire">Hire</option>
                  <option value="NoHire">No Hire</option>
                  <option value="StrongNoHire">Strong No Hire</option>
                </select>
              </div>

              <div>
                <label className="text-sm text-muted-foreground">Comments</label>
                <textarea 
                  className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={feedbackForm.comments}
                  onChange={e => setFeedbackForm({...feedbackForm, comments: e.target.value})}
                />
              </div>

              <Button type="submit">Submit Feedback</Button>
            </form>
          </CardContent>
        </Card>
      )}

      {interview.feedbacks && interview.feedbacks.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Submitted Feedbacks</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {interview.feedbacks.map(f => (
              <div key={f.id} className="border p-4 rounded-md">
                <div className="flex justify-between items-start mb-2">
                  <div className="font-semibold">{f.reviewerName}</div>
                  <Badge>{f.recommendation}</Badge>
                </div>
                <div className="text-sm mb-2 text-muted-foreground">Overall Score: {f.overallScore}/10</div>
                {f.comments && <div className="text-sm whitespace-pre-wrap">{f.comments}</div>}
              </div>
            ))}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
