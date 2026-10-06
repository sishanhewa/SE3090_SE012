using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Applications;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobRepository _jobRepository;
    private readonly AppDbContext _context;

    public ApplicationService(IApplicationRepository applicationRepository, IJobRepository jobRepository, AppDbContext context)
    {
        _applicationRepository = applicationRepository;
        _jobRepository = jobRepository;
        _context = context;
    }

    public async Task<Result<ApplicationResponse>> CreateApplicationAsync(Guid jobId, CreateApplicationRequest request, Guid candidateProfileId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return Result<ApplicationResponse>.NotFound("Job not found");

        if (job.Status != JobStatus.Published)
            return Result<ApplicationResponse>.Failure("Job is not currently accepting applications");

        if (job.ApplicationDeadline.HasValue && job.ApplicationDeadline.Value < DateTime.UtcNow)
            return Result<ApplicationResponse>.Failure("The application deadline for this job has passed.");

        var hasApplied = await _applicationRepository.HasAppliedAsync(jobId, candidateProfileId, cancellationToken);
        if (hasApplied)
            return Result<ApplicationResponse>.Conflict("You have already applied for this job");

        if (request.ResumeDocumentId.HasValue && !await _context.CandidateDocuments.AnyAsync(
                d => d.Id == request.ResumeDocumentId.Value && d.CandidateProfileId == candidateProfileId && d.FileType == "CV",
                cancellationToken))
            return Result<ApplicationResponse>.Failure("The selected CV does not belong to this candidate.");

        var application = new Domain.Entities.Application
        {
            JobId = jobId,
            CandidateProfileId = candidateProfileId,
            CoverLetter = request.CoverLetter,
            ResumeDocumentId = request.ResumeDocumentId,
            Status = ApplicationStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        application.History.Add(new ApplicationHistory
        {
            FromStatus = ApplicationStatus.Submitted,
            ToStatus = ApplicationStatus.Submitted,
            Notes = "Application submitted"
        });

        await _applicationRepository.AddAsync(application, cancellationToken);

        return Result<ApplicationResponse>.Success(MapToResponse(application));
    }

    public async Task<Result<ApplicationResponse>> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetApplicationWithDetailsAsync(id, cancellationToken);
        if (application == null)
            return Result<ApplicationResponse>.NotFound("Application not found");

        return Result<ApplicationResponse>.Success(MapToResponse(application));
    }

    public async Task<Result<PagedResult<ApplicationResponse>>> GetApplicationsAsync(PaginationParams paginationParams, Guid? jobId = null, Guid? candidateProfileId = null, CancellationToken cancellationToken = default, Guid? companyId = null)
    {
        if (companyId.HasValue)
        {
            var query = _context.Applications
                .Include(a => a.Job).ThenInclude(j => j.Company)
                .Include(a => a.CandidateProfile).ThenInclude(cp => cp.User)
                .Where(a => a.Job.CompanyId == companyId.Value);
            if (jobId.HasValue) query = query.Where(a => a.JobId == jobId.Value);
            if (candidateProfileId.HasValue) query = query.Where(a => a.CandidateProfileId == candidateProfileId.Value);
            var count = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(a => a.SubmittedAt)
                .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize).ToListAsync(cancellationToken);
            return Result<PagedResult<ApplicationResponse>>.Success(new PagedResult<ApplicationResponse>(
                items.Select(MapToResponse).ToList(), count, paginationParams.Page, paginationParams.PageSize));
        }
        var result = await _applicationRepository.GetApplicationsAsync(paginationParams, jobId, candidateProfileId, null, cancellationToken);
        
        var response = new PagedResult<ApplicationResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        );

        return Result<PagedResult<ApplicationResponse>>.Success(response);
    }

    public async Task<Result<PagedResult<ApplicationResponse>>> GetMyApplicationsAsync(Guid candidateProfileId, PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        var result = await _applicationRepository.GetApplicationsAsync(paginationParams, null, candidateProfileId, null, cancellationToken);
        
        var response = new PagedResult<ApplicationResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        );

        return Result<PagedResult<ApplicationResponse>>.Success(response);
    }

    public async Task<Result> UpdateApplicationStatusAsync(Guid id, ApplicationStatus newStatus, string changedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken);
        if (application == null)
            return Result.NotFound("Application not found");

        var oldStatus = application.Status;
        if (oldStatus == newStatus)
            return Result.Success(); // No change

        // Validate state transition
        bool isValidTransition = false;
        switch (oldStatus)
        {
            case ApplicationStatus.Submitted:
                isValidTransition = newStatus == ApplicationStatus.Screening;
                break;
            case ApplicationStatus.Screening:
                // Screening decisions are recorded through the reviewed AI workflow.
                isValidTransition = false;
                break;
            case ApplicationStatus.Shortlisted:
                isValidTransition = newStatus == ApplicationStatus.Rejected;
                break;
            case ApplicationStatus.Interview:
                isValidTransition = newStatus == ApplicationStatus.Rejected;
                break;
            case ApplicationStatus.Offered:
                isValidTransition = false;
                break;
        }

        if (!isValidTransition)
            return Result.Failure($"Invalid status transition from {oldStatus} to {newStatus}", "InvalidStateTransition");
        if (newStatus == ApplicationStatus.Rejected && string.IsNullOrWhiteSpace(notes))
            return Result.Failure("A reason is required when rejecting a candidate.");

        application.Status = newStatus;
        
        application.History.Add(new ApplicationHistory
        {
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Notes = notes ?? $"Status updated to {newStatus} by {changedBy}"
        });

        await _applicationRepository.UpdateAsync(application, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> WithdrawApplicationAsync(Guid id, Guid candidateProfileId, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken);
        if (application == null)
            return Result.NotFound("Application not found");

        if (application.CandidateProfileId != candidateProfileId)
            return Result.Forbidden();

        if (application.Status == ApplicationStatus.Rejected || application.Status == ApplicationStatus.Hired || application.Status == ApplicationStatus.Withdrawn)
            return Result.Failure("Cannot withdraw an application in its current state");

        var oldStatus = application.Status;
        application.Status = ApplicationStatus.Withdrawn;
        
        application.History.Add(new ApplicationHistory
        {
            FromStatus = oldStatus,
            ToStatus = ApplicationStatus.Withdrawn,
            Notes = "Withdrawn by candidate"
        });

        await _applicationRepository.UpdateAsync(application, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<List<ApplicationHistoryResponse>>> GetApplicationHistoryAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var history = await _context.ApplicationHistory
            .Where(h => h.ApplicationId == applicationId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new ApplicationHistoryResponse
            {
                Id = h.Id,
                FromStatus = h.FromStatus.ToString(),
                ToStatus = h.ToStatus.ToString(),
                ChangedBy = h.ChangedBy,
                Notes = h.Notes,
                ChangedAt = h.ChangedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<ApplicationHistoryResponse>>.Success(history);
    }

    public async Task<Result<List<DocumentResponse>>> GetApplicationDocumentsAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
            return Result<List<DocumentResponse>>.NotFound("Application not found.");

        var docs = application.CandidateProfile.Documents
            .Select(d => new DocumentResponse
            {
                Id = d.Id,
                FileName = d.FileName,
                FileUrl = d.FileUrl,
                FileType = d.FileType,
                FileSizeBytes = d.FileSizeBytes,
                CreatedAt = d.CreatedAt
            })
            .ToList();

        return Result<List<DocumentResponse>>.Success(docs);
    }

    public async Task<Result<ResumeDownload>> GetResumeAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
        if (application == null)
            return Result<ResumeDownload>.NotFound("Application not found.");
        if (!application.ResumeDocumentId.HasValue)
            return Result<ResumeDownload>.NotFound("No CV is attached to this application.");

        var document = await _context.CandidateDocuments.FirstOrDefaultAsync(
            d => d.Id == application.ResumeDocumentId && d.CandidateProfileId == application.CandidateProfileId,
            cancellationToken);
        if (document == null || document.FileType != "CV")
            return Result<ResumeDownload>.NotFound("The attached CV was not found.");

        return await GetDocumentAsync(applicationId, document.Id, cancellationToken);
    }

    public async Task<Result<ResumeDownload>> GetDocumentAsync(Guid applicationId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
        if (application == null)
            return Result<ResumeDownload>.NotFound("Application not found.");
        var document = await _context.CandidateDocuments.FirstOrDefaultAsync(
            d => d.Id == documentId && d.CandidateProfileId == application.CandidateProfileId,
            cancellationToken);
        if (document == null)
            return Result<ResumeDownload>.NotFound("Document not found.");

        var path = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "documents", Path.GetFileName(document.FileUrl));
        if (!File.Exists(path))
            return Result<ResumeDownload>.NotFound("The document file is missing from storage.");

        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        return Result<ResumeDownload>.Success(new ResumeDownload
        {
            Content = await File.ReadAllBytesAsync(path, cancellationToken),
            FileName = document.FileName,
            ContentType = contentType
        });
    }

    public async Task<Result<DocumentResponse>> UploadDocumentAsync(Guid applicationId, Guid userId, Stream fileStream, string fileName, long fileSize, CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications
            .Include(a => a.CandidateProfile)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
            return Result<DocumentResponse>.NotFound("Application not found.");

        if (application.CandidateProfile.UserId != userId)
            return Result<DocumentResponse>.Forbidden();

        if (fileSize <= 0 || fileSize > 10_000_000)
            return Result<DocumentResponse>.Failure("File must be smaller than 10 MB.");

        // Determine file type from extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".pdf" or ".docx" or ".jpg" or ".jpeg" or ".png"))
            return Result<DocumentResponse>.Failure("Unsupported document type.");
        var fileType = extension switch
        {
            ".pdf" => "CV",
            ".docx" => "CV",
            ".jpg" or ".jpeg" or ".png" => "Certificate",
            _ => "Document"
        };

        if (fileType == "CV" && application.Status != ApplicationStatus.Submitted &&
            application.Status != ApplicationStatus.Screening)
            return Result<DocumentResponse>.Failure("CV changes are closed after screening review.");
        if (fileType == "CV" && await _context.WorkflowExecutions.AnyAsync(w =>
                w.RelatedEntityId == applicationId &&
                (w.Status == WorkflowStatus.Planning || w.Status == WorkflowStatus.InProgress ||
                 w.Status == WorkflowStatus.AwaitingApproval), cancellationToken))
            return Result<DocumentResponse>.Conflict("CV cannot be replaced while screening is running or awaiting review.");

        // Save to local storage (uploads directory)
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "documents");
        Directory.CreateDirectory(uploadsDir);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream, cancellationToken);
        }

        var document = new CandidateDocument
        {
            CandidateProfileId = application.CandidateProfileId,
            FileName = Path.GetFileName(fileName),
            FileUrl = $"/uploads/documents/{uniqueFileName}",
            FileType = fileType,
            FileSizeBytes = fileSize
        };

        _context.CandidateDocuments.Add(document);
        if (fileType == "CV")
            application.ResumeDocumentId = document.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<DocumentResponse>.Success(new DocumentResponse
        {
            Id = document.Id,
            FileName = document.FileName,
            FileUrl = document.FileUrl,
            FileType = document.FileType,
            FileSizeBytes = document.FileSizeBytes,
            CreatedAt = document.CreatedAt
        });
    }

    private static ApplicationResponse MapToResponse(Domain.Entities.Application application)
    {
        return new ApplicationResponse
        {
            Id = application.Id,
            JobId = application.JobId,
            JobTitle = application.Job?.Title ?? string.Empty,
            CompanyName = application.Job?.Company?.Name ?? string.Empty,
            CandidateProfileId = application.CandidateProfileId,
            CandidateName = application.CandidateProfile?.User?.FirstName + " " + application.CandidateProfile?.User?.LastName,
            Status = application.Status.ToString(),
            SubmittedAt = application.SubmittedAt,
            CoverLetter = application.CoverLetter,
            ResumeDocumentId = application.ResumeDocumentId,
            AiScore = application.AiScore,
            AiRecommendation = application.AiRecommendation,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt
        };
    }
}
