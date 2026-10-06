using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

public class GoogleCalendarServiceTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? EventJson { get; private set; }
        public Uri? EventUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.Host == "oauth2.googleapis.com")
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                { Content = new StringContent("{\"access_token\":\"test-token\"}") };

            EventUri = request.RequestUri;
            EventJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var payload = JsonDocument.Parse(EventJson);
            var eventId = payload.RootElement.GetProperty("id").GetString();
            var start = payload.RootElement.GetProperty("start").GetProperty("dateTime").GetString();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = eventId,
                    start = new { dateTime = start },
                    attendees = new[] { new { email = "candidate@example.com" } }
                }))
            };
        }
    }

    private sealed class OAuthUpdateHandler : HttpMessageHandler
    {
        public System.Net.HttpStatusCode UpdateStatus { get; set; } = System.Net.HttpStatusCode.OK;
        public string? TokenBody { get; private set; }
        public string? UpdateBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.Host == "oauth2.googleapis.com")
            {
                TokenBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    { Content = new StringContent("{\"access_token\":\"organizer-token\"}") };
            }
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("organizer-token", request.Headers.Authorization?.Parameter);
            UpdateBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(UpdateStatus)
                { Content = new StringContent(UpdateStatus == System.Net.HttpStatusCode.OK ? "{}" : "error") };
        }
    }

    private sealed class FreeBusyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.RequestUri!.Host == "oauth2.googleapis.com"
                ? "{\"access_token\":\"organizer-token\"}"
                : "{\"calendars\":{\"primary\":{\"busy\":[{\"start\":\"2026-10-08T15:00:00+05:30\",\"end\":\"2026-10-08T16:00:00+05:30\"}]}}}";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                { Content = new StringContent(content) });
        }
    }

    [Fact]
    public async Task FreeBusySlotsAreConvertedToUtcBeforeConflictChecking()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GoogleCalendar:CalendarId"] = "primary",
            ["GoogleCalendar:OAuthClientId"] = "client-id",
            ["GoogleCalendar:OAuthClientSecret"] = "client-secret",
            ["GoogleCalendar:OAuthRefreshToken"] = "refresh-token"
        }).Build();
        var service = new GoogleCalendarService(config, NullLogger<GoogleCalendarService>.Instance,
            new HttpClient(new FreeBusyHandler()));

        var result = await service.GetBusySlotsAsync("primary",
            new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 10, 8, 9, 30, 0, DateTimeKind.Utc), result.Data![0].Start);
    }

    [Fact]
    public async Task OrganizerOAuthUpdatesCalendarOnlyWhenGoogleConfirmsSuccess()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GoogleCalendar:CalendarId"] = "primary",
            ["GoogleCalendar:OAuthClientId"] = "client-id",
            ["GoogleCalendar:OAuthClientSecret"] = "client-secret",
            ["GoogleCalendar:OAuthRefreshToken"] = "refresh-token"
        }).Build();
        var handler = new OAuthUpdateHandler();
        var service = new GoogleCalendarService(config, NullLogger<GoogleCalendarService>.Instance,
            new HttpClient(handler));
        var start = DateTime.UtcNow.AddDays(2);
        var request = new CalendarEventRequest
        {
            Title = "Rescheduled interview", StartTime = start, EndTime = start.AddHours(1),
            AttendeeEmails = new List<string> { "candidate@example.com" }
        };

        var confirmed = await service.UpdateInterviewEventAsync("event-id", request);
        Assert.True(confirmed.IsSuccess);
        Assert.Contains("grant_type=refresh_token", handler.TokenBody);
        using (var payload = JsonDocument.Parse(handler.UpdateBody!))
            Assert.Equal(start.AddHours(1).ToString("o"),
                payload.RootElement.GetProperty("end").GetProperty("dateTime").GetString());

        handler.UpdateStatus = System.Net.HttpStatusCode.BadGateway;
        var failed = await service.UpdateInterviewEventAsync("event-id", request);
        Assert.False(failed.IsSuccess);
    }

    [Fact]
    public async Task MissingCredentialsNeverReportInvitationOrAvailabilityAsSuccessful()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["GoogleCalendar:CalendarId"] = "primary" }).Build();
        var service = new GoogleCalendarService(config, NullLogger<GoogleCalendarService>.Instance,
            new HttpClient());

        var invitation = await service.CreateInterviewEventAsync(new CalendarEventRequest
        {
            Title = "Interview", StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            AttendeeEmails = new List<string> { "candidate@example.com" }
        });
        var availability = await service.GetBusySlotsAsync("primary", DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1));

        Assert.False(invitation.IsSuccess);
        Assert.False(availability.IsSuccess);
    }

    [Fact]
    public async Task ConfiguredCalendarSignsTokenAndSendsCandidateWithCorrectEndTime()
    {
        using var rsa = RSA.Create(2048);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GoogleCalendar:CalendarId"] = "primary",
            ["GoogleCalendar:ServiceAccountEmail"] = "service@example.test",
            ["GoogleCalendar:DelegatedUserEmail"] = "recruiter@example.test",
            ["GoogleCalendar:PrivateKey"] = rsa.ExportPkcs8PrivateKeyPem()
        }).Build();
        var handler = new RecordingHandler();
        var service = new GoogleCalendarService(config, NullLogger<GoogleCalendarService>.Instance,
            new HttpClient(handler));
        var start = DateTime.UtcNow.AddDays(2);
        var result = await service.CreateInterviewEventAsync(new CalendarEventRequest
        {
            EventId = "tf1234567890abcdef1234567890abcdef",
            Title = "Interview", StartTime = start, EndTime = start.AddHours(1),
            AttendeeEmails = new List<string> { "candidate@example.com" }
        });

        Assert.True(result.IsSuccess);
        Assert.Contains("sendUpdates=all", handler.EventUri!.Query);
        using var document = JsonDocument.Parse(handler.EventJson!);
        Assert.Equal("tf1234567890abcdef1234567890abcdef",
            document.RootElement.GetProperty("id").GetString());
        Assert.Equal(start.AddHours(1).ToString("o"),
            document.RootElement.GetProperty("end").GetProperty("dateTime").GetString());
        Assert.Equal("candidate@example.com",
            document.RootElement.GetProperty("attendees")[0].GetProperty("email").GetString());
    }
}
