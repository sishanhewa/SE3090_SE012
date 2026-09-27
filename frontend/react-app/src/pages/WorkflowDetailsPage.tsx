import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { workflowsApi, type WorkflowExecutionResponse } from '../api/workflowsApi';
import { useAuthStore } from '../store/authStore';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ArrowLeft, CheckCircle, AlertTriangle, XCircle, Clock, Check } from 'lucide-react';
import { Textarea } from '@/components/ui/textarea';

export default function WorkflowDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuthStore();
  
  const roles = user?.roles ?? [];
  const isManager = roles.some((r) => ['SystemAdmin', 'HiringManager'].includes(r));

  const [workflow, setWorkflow] = useState<WorkflowExecutionResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [comments, setComments] = useState('');

  const fetchWorkflow = async () => {
    if (!id) return;
    try {
      const data = await workflowsApi.getById(id);
      setWorkflow(data);
    } catch (error) {
      console.error('Failed to fetch workflow', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWorkflow();
  }, [id]);

  const handleApprove = async () => {
    if (!id) return;
    try {
      await workflowsApi.approve(id, comments);
      alert('Workflow approved successfully');
      fetchWorkflow();
    } catch (error) {
      console.error('Approval failed', error);
      alert('Approval failed. Please ensure you have the required permissions.');
    }
  };

  const handleReject = async () => {
    if (!id) return;
    try {
      await workflowsApi.reject(id, comments);
      alert('Workflow rejected');
      fetchWorkflow();
    } catch (error) {
      console.error('Rejection failed', error);
      alert('Rejection failed.');
    }
  };

  const handleRevise = async () => {
    if (!id) return;
    if (!comments.trim()) {
      alert('Comments are required when requesting a revision.');
      return;
    }
    try {
      await workflowsApi.requestRevision(id, comments);
      alert('Revision requested');
      fetchWorkflow();
    } catch (error) {
      console.error('Revision request failed', error);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading workflow details...</div>;
  }

  if (!workflow) {
    return <div className="p-8 text-center text-destructive">Workflow not found.</div>;
  }

  // Parse final result for candidate evaluation summary if present
  let parsedResult = null;
  if (workflow.finalResult) {
    try {
      parsedResult = JSON.parse(workflow.finalResult);
    } catch (e) {
      // Ignore
    }
  }

  return (
    <div className="p-8 space-y-6 max-w-5xl mx-auto">
      <Button variant="ghost" onClick={() => navigate(-1)} className="mb-4 gap-2">
        <ArrowLeft className="h-4 w-4" /> Back to Workflows
      </Button>

      <div className="flex justify-between items-start">
        <div>
          <h2 className="text-3xl font-bold tracking-tight mb-2">Workflow Execution</h2>
          <p className="text-muted-foreground">Started on {new Date(workflow.createdAt).toLocaleString()}</p>
        </div>
        <Badge className="text-lg py-1 px-4" variant={
          workflow.status === 'Completed' || workflow.status === 'Approved' ? 'default' : 
          workflow.status === 'Failed' || workflow.status === 'Rejected' ? 'destructive' : 
          workflow.status === 'AwaitingApproval' ? 'secondary' : 'outline'
        }>
          {workflow.status}
        </Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Objective</CardTitle>
        </CardHeader>
        <CardContent>
          <p>{workflow.objective}</p>
        </CardContent>
      </Card>

      {parsedResult && (
        <Card className="border-purple-200 bg-purple-50 dark:bg-purple-900/10 dark:border-purple-800">
          <CardHeader>
            <CardTitle className="text-purple-800 dark:text-purple-300">Final Recommendation</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <span className="font-semibold text-purple-900 dark:text-purple-200 block mb-1">Overall Recommendation:</span>
              <p className="text-purple-800 dark:text-purple-300 text-lg font-medium">{parsedResult.overall_recommendation}</p>
            </div>
            <div>
              <span className="font-semibold text-purple-900 dark:text-purple-200 block mb-1">AI Summary:</span>
              <p className="text-purple-800 dark:text-purple-300">{parsedResult.summary}</p>
            </div>
            
            {/* Show proposed slots if any */}
            {parsedResult.interview_proposal?.proposed_slots?.length > 0 && (
              <div className="mt-4 p-4 bg-white dark:bg-black/20 rounded-md border border-purple-100 dark:border-purple-800/50">
                <span className="font-semibold block mb-2">Proposed Interview Slots (No Conflicts):</span>
                <ul className="space-y-2">
                  {parsedResult.interview_proposal.proposed_slots.map((slot: any, idx: number) => (
                    <li key={idx} className="flex items-center gap-2 text-sm">
                      <Clock className="h-4 w-4 text-purple-500" />
                      {new Date(slot.start_time).toLocaleString()} - {new Date(slot.end_time).toLocaleTimeString()}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {workflow.status === 'AwaitingApproval' && isManager && (
        <Card className="border-amber-200 bg-amber-50 dark:bg-amber-900/10 dark:border-amber-800/50">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-amber-800 dark:text-amber-400">
              <AlertTriangle className="h-5 w-5" />
              Action Required
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-amber-800 dark:text-amber-300 mb-4">
              The AI workflow has completed and requires human approval before proceeding (e.g., creating the interview in the calendar and notifying the candidate).
            </p>
            <Textarea 
              placeholder="Add comments or feedback (required for revisions)..." 
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              className="bg-white dark:bg-background"
            />
            <div className="flex gap-4 pt-2">
              <Button onClick={handleApprove} className="gap-2 bg-green-600 hover:bg-green-700">
                <Check className="h-4 w-4" /> Approve & Schedule
              </Button>
              <Button onClick={handleRevise} variant="outline" className="gap-2">
                <ArrowLeft className="h-4 w-4" /> Request AI Revision
              </Button>
              <Button onClick={handleReject} variant="destructive" className="gap-2">
                <XCircle className="h-4 w-4" /> Reject
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="space-y-6">
          <h3 className="text-xl font-bold">Execution Timeline (Agent Steps)</h3>
          {workflow.agentSteps.length === 0 ? (
            <p className="text-muted-foreground italic">No steps recorded.</p>
          ) : (
            <div className="space-y-4 relative before:absolute before:inset-0 before:ml-5 before:-translate-x-px md:before:mx-auto md:before:translate-x-0 before:h-full before:w-0.5 before:bg-gradient-to-b before:from-transparent before:via-muted before:to-transparent">
              {workflow.agentSteps.map((step, idx) => (
                <Card key={step.id} className="relative z-10 ml-12 md:ml-0 border-l-4 border-l-purple-500">
                  <div className="absolute top-4 -left-14 md:-left-6 w-8 h-8 bg-background border-2 border-purple-500 rounded-full flex items-center justify-center font-bold text-sm text-purple-600">
                    {idx + 1}
                  </div>
                  <CardHeader className="py-3">
                    <div className="flex justify-between items-center">
                      <CardTitle className="text-base font-semibold text-purple-700 dark:text-purple-400">{step.agentName}</CardTitle>
                      <Badge variant="outline">{step.status}</Badge>
                    </div>
                  </CardHeader>
                  <CardContent className="py-2 text-sm text-muted-foreground space-y-3">
                    <div>
                      <span className="font-semibold block text-foreground mb-1">Tools Called:</span>
                      {step.toolCalls.length === 0 ? (
                        <span className="italic">None</span>
                      ) : (
                        <ul className="list-disc pl-4 space-y-1">
                          {step.toolCalls.map(tc => (
                            <li key={tc.id}>
                              <code className="bg-muted px-1.5 py-0.5 rounded text-xs">{tc.toolName}</code>
                              <span className="text-xs text-muted-foreground ml-2">({tc.durationMs}ms)</span>
                              {tc.validated === false && <Badge variant="destructive" className="ml-2 text-[10px] px-1 py-0">Failed</Badge>}
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                    {step.errorDetails && (
                      <div className="p-2 bg-destructive/10 text-destructive rounded-md">
                        {step.errorDetails}
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </div>

        <div className="space-y-6">
          <h3 className="text-xl font-bold">Validation Checks</h3>
          {workflow.validationResults.length === 0 ? (
            <p className="text-muted-foreground italic">No validation results recorded.</p>
          ) : (
            <div className="space-y-3">
              {workflow.validationResults.map((vr) => (
                <div key={vr.id} className="flex items-start gap-3 p-3 border rounded-md">
                  {vr.passed ? (
                    <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
                  ) : (
                    <XCircle className="h-5 w-5 text-destructive mt-0.5" />
                  )}
                  <div className="flex-1">
                    <div className="font-semibold">{vr.validationType}</div>
                    {vr.errors && <div className="text-sm text-destructive mt-1">{vr.errors}</div>}
                    {vr.warnings && <div className="text-sm text-amber-600 mt-1">{vr.warnings}</div>}
                  </div>
                </div>
              ))}
            </div>
          )}

          {workflow.approvals.length > 0 && (
            <>
              <h3 className="text-xl font-bold mt-8">Approval History</h3>
              <div className="space-y-3">
                {workflow.approvals.map((approval) => (
                  <Card key={approval.id}>
                    <CardContent className="p-4 space-y-2">
                      <div className="flex justify-between items-start">
                        <span className="font-medium">{approval.requestedAction}</span>
                        <Badge variant={
                          approval.decision === 'Approved' ? 'default' :
                          approval.decision === 'Rejected' ? 'destructive' :
                          approval.decision === 'RevisionRequested' ? 'outline' : 'secondary'
                        }>
                          {approval.decision || 'Pending'}
                        </Badge>
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Requested: {new Date(approval.requestedAt).toLocaleString()}
                        {approval.decidedAt && ` • Decided: ${new Date(approval.decidedAt).toLocaleString()}`}
                      </div>
                      {approval.comments && (
                        <div className="text-sm mt-2 p-2 bg-muted rounded-md italic">
                          "{approval.comments}"
                        </div>
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
