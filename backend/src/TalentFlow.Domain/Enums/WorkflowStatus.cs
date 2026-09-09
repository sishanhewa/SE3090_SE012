namespace TalentFlow.Domain.Enums;

public enum WorkflowStatus
{
    Planning,
    InProgress,
    AwaitingApproval,
    Approved,
    Rejected,
    RevisionRequested,
    Completed,
    Failed,
    Cancelled
}
