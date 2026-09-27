import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { workflowsApi, type WorkflowExecutionResponse } from '../api/workflowsApi';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Bot, FileText, ArrowRight } from 'lucide-react';

export default function WorkflowsPage() {
  const navigate = useNavigate();
  const [workflows, setWorkflows] = useState<WorkflowExecutionResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchWorkflows();
  }, []);

  const fetchWorkflows = async () => {
    try {
      const data = await workflowsApi.getAll();
      setWorkflows(data);
    } catch (error) {
      console.error('Failed to fetch workflows', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading AI workflows...</div>;
  }

  return (
    <div className="p-8 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">AI Workflows</h2>
          <p className="text-muted-foreground mt-2">Manage Agentic AI screening workflows and approvals.</p>
        </div>
      </div>

      {workflows.length === 0 ? (
        <Card className="text-center py-12">
          <CardContent>
            <Bot className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No Workflows Found</h3>
            <p className="text-muted-foreground">Start an AI screening from the Applications page.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {workflows.map((workflow) => (
            <Card key={workflow.id} className="flex flex-col hover:border-purple-300 transition-colors cursor-pointer" onClick={() => navigate(`/workflows/${workflow.id}`)}>
              <CardHeader className="pb-3">
                <div className="flex justify-between items-start">
                  <div className="flex items-center gap-2">
                    <Bot className="h-5 w-5 text-purple-600" />
                    <CardTitle className="text-lg truncate" title={workflow.objective}>Screening Workflow</CardTitle>
                  </div>
                  <Badge variant={
                    workflow.status === 'Completed' || workflow.status === 'Approved' ? 'default' :
                    workflow.status === 'Failed' || workflow.status === 'Rejected' ? 'destructive' :
                    workflow.status === 'AwaitingApproval' ? 'secondary' : 'outline'
                  }>
                    {workflow.status}
                  </Badge>
                </div>
                <p className="text-xs text-muted-foreground mt-2 line-clamp-2">
                  {workflow.objective}
                </p>
              </CardHeader>
              <CardContent className="flex-1">
                <div className="space-y-3 mt-2">
                  <div className="flex items-center gap-2 text-sm">
                    <FileText className="h-4 w-4 text-muted-foreground" />
                    <span className="text-muted-foreground truncate">{workflow.agentSteps.length} Agent Steps executed</span>
                  </div>
                </div>
              </CardContent>
              <div className="p-4 pt-0 mt-auto">
                <Button variant="ghost" className="w-full justify-between" onClick={() => navigate(`/workflows/${workflow.id}`)}>
                  View Details <ArrowRight className="h-4 w-4" />
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
