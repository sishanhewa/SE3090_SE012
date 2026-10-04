import { useApplicationHistory, type ApplicationHistoryItem } from '../../api/applicationDetailsApi';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { CheckCircle2, Clock, ArrowRight } from 'lucide-react';

const STATUS_COLORS: Record<string, string> = {
  Submitted: 'bg-blue-500',
  Screening: 'bg-purple-500',
  Shortlisted: 'bg-yellow-500',
  Interview: 'bg-orange-500',
  Offered: 'bg-emerald-500',
  Hired: 'bg-green-600',
  Rejected: 'bg-red-500',
  Withdrawn: 'bg-gray-500',
};

function getStatusDot(status: string) {
  return STATUS_COLORS[status] || 'bg-gray-400';
}

interface ApplicationTimelineProps {
  applicationId: string;
}

export default function ApplicationTimeline({ applicationId }: ApplicationTimelineProps) {
  const { history, loading } = useApplicationHistory(applicationId);

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Application Timeline</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-muted-foreground">
            <Clock className="h-4 w-4 animate-spin" />
            Loading timeline...
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!history.length) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Application Timeline</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground">No history available.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Application Timeline</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="relative">
          {/* Vertical line */}
          <div className="absolute left-3 top-0 bottom-0 w-0.5 bg-border" />

          <div className="space-y-6">
            {history.map((item: ApplicationHistoryItem, index: number) => (
              <div key={item.id} className="relative flex gap-4 items-start">
                {/* Dot */}
                <div
                  className={`relative z-10 w-6 h-6 rounded-full flex items-center justify-center ${
                    index === history.length - 1
                      ? getStatusDot(item.toStatus) + ' ring-2 ring-offset-2 ring-primary'
                      : getStatusDot(item.toStatus)
                  }`}
                >
                  <CheckCircle2 className="h-3 w-3 text-white" />
                </div>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 text-sm font-medium">
                    <span className="text-muted-foreground">{item.fromStatus}</span>
                    <ArrowRight className="h-3 w-3 text-muted-foreground" />
                    <span className="font-semibold">{item.toStatus}</span>
                  </div>
                  {item.notes && (
                    <p className="text-sm text-muted-foreground mt-1">{item.notes}</p>
                  )}
                  <p className="text-xs text-muted-foreground mt-1">
                    {new Date(item.changedAt).toLocaleString()}
                    {item.changedBy && <span> · by {item.changedBy}</span>}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
