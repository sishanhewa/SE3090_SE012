using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;
using TalentFlow.Infrastructure.Services;
using TalentFlow.Application.Interfaces.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

/// <summary>
/// Tests for AI workflow service.
/// </summary>
public class WorkflowServiceTests
{
    private AppDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetWorkflow_NonExistent_ReturnsNotFound()
    {
        using var context = CreateTestContext();
        var service = new WorkflowService(context, null!);

        var result = await service.GetWorkflowAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApproveWorkflow_WrongStatus_ReturnsFail()
    {
        using var context = CreateTestContext();

        // Create a workflow in Planning status (not AwaitingApproval)
        var workflow = new WorkflowExecution
        {
            Objective = "Test workflow",
            Status = WorkflowStatus.Planning,
            InitiatedById = Guid.NewGuid(),
        };
        context.WorkflowExecutions.Add(workflow);
        await context.SaveChangesAsync();

        var service = new WorkflowService(context, null!);
        var result = await service.ApproveWorkflowAsync(
            workflow.Id, Guid.NewGuid(), "test comment");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RejectWorkflow_WrongStatus_ReturnsFail()
    {
        using var context = CreateTestContext();

        var workflow = new WorkflowExecution
        {
            Objective = "Test workflow",
            Status = WorkflowStatus.Completed,
            InitiatedById = Guid.NewGuid(),
        };
        context.WorkflowExecutions.Add(workflow);
        await context.SaveChangesAsync();

        var service = new WorkflowService(context, null!);
        var result = await service.RejectWorkflowAsync(
            workflow.Id, Guid.NewGuid(), "test reason");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RejectWorkflow_RecordsHumanDecisionAndRejectsApplication()
    {
        using var context = CreateTestContext();
        var application = new TalentFlow.Domain.Entities.Application
        {
            JobId = Guid.NewGuid(), CandidateProfileId = Guid.NewGuid(), Status = ApplicationStatus.Screening
        };
        var workflow = new WorkflowExecution
        {
            Objective = "Review CV evidence", Status = WorkflowStatus.AwaitingApproval,
            InitiatedById = Guid.NewGuid(), RelatedEntityType = "Application", RelatedEntityId = application.Id
        };
        context.Applications.Add(application);
        context.WorkflowExecutions.Add(workflow);
        await context.SaveChangesAsync();

        var result = await new WorkflowService(context, null!).RejectWorkflowAsync(
            workflow.Id, Guid.NewGuid(), "Required CV experience was not evidenced.");

        Assert.True(result.IsSuccess);
        Assert.Equal(ApplicationStatus.Rejected, application.Status);
        Assert.Contains(context.ApplicationHistory, h => h.ApplicationId == application.Id &&
            h.ToStatus == ApplicationStatus.Rejected);
        Assert.Contains(context.WorkflowApprovals, a => a.WorkflowExecutionId == workflow.Id &&
            a.Decision == "RejectedCandidate");
    }

    [Fact]
    public async Task RejectWorkflow_RequiresReason()
    {
        using var context = CreateTestContext();
        var workflow = new WorkflowExecution
        {
            Objective = "Review CV evidence", Status = WorkflowStatus.AwaitingApproval,
            InitiatedById = Guid.NewGuid()
        };
        context.WorkflowExecutions.Add(workflow);
        await context.SaveChangesAsync();

        var result = await new WorkflowService(context, null!).RejectWorkflowAsync(
            workflow.Id, Guid.NewGuid(), " ");

        Assert.False(result.IsSuccess);
        Assert.Equal(WorkflowStatus.AwaitingApproval, workflow.Status);
    }
}
