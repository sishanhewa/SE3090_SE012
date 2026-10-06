using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentFlow.Application.Common;

namespace TalentFlow.Infrastructure.Services;

public class CandidateNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CandidateNotificationService> _logger;

    public CandidateNotificationService(IConfiguration configuration, ILogger<CandidateNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result> SendHiredAsync(string email, string candidateName, string position,
        DateTime startDate, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var from = _configuration["Smtp:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            return Result.Failure("Hiring email is pending because SMTP is not configured.");

        try
        {
            using var message = new MailMessage(from, email)
            {
                Subject = $"Welcome to the team — {position}",
                Body = $"Hello {candidateName},\n\nYour offer for {position} has been accepted and your hiring is confirmed. " +
                    $"Your planned start date is {startDate:yyyy-MM-dd}. Please sign in to view your onboarding tasks.\n\nBest wishes,\nHiring team",
                IsBodyHtml = false
            };
            using var client = new SmtpClient(host, _configuration.GetValue("Smtp:Port", 587))
            {
                EnableSsl = _configuration.GetValue("Smtp:EnableSsl", true)
            };
            var username = _configuration["Smtp:Username"];
            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, _configuration["Smtp:Password"]);
            await client.SendMailAsync(message, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send hired notification to application candidate");
            return Result.Failure("Hiring email could not be delivered. Retry through the configured mail service.");
        }
    }
}
