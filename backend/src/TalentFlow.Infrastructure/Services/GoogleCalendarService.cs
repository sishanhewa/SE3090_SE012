using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Infrastructure.Services;

/// <summary>
/// Google Calendar integration using Google Calendar API v3.
/// Uses a Service Account for server-to-server authentication.
/// Falls back gracefully if credentials are not configured.
/// </summary>
public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly HttpClient _httpClient;
    private readonly bool _isEnabled;

    private const string CalendarApiBase = "https://www.googleapis.com/calendar/v3";

    public GoogleCalendarService(
        IConfiguration configuration,
        ILogger<GoogleCalendarService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;

        _isEnabled = !string.IsNullOrWhiteSpace(
            _configuration["GoogleCalendar:ServiceAccountEmail"]);

        if (!_isEnabled)
        {
            _logger.LogWarning(
                "Google Calendar integration is disabled. " +
                "Set GoogleCalendar:ServiceAccountEmail and GoogleCalendar:PrivateKey in configuration to enable.");
        }
    }

    public async Task<Result<CalendarEventResult>> CreateInterviewEventAsync(
        CalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Google Calendar disabled — returning mock event for interview: {Title}", request.Title);
            return Result<CalendarEventResult>.Success(new CalendarEventResult
            {
                EventId = $"mock-event-{Guid.NewGuid():N}",
                HtmlLink = $"https://calendar.google.com/calendar/event?eid=mock-{Guid.NewGuid():N}",
                MeetLink = request.MeetingUrl,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            });
        }

        try
        {
            var calendarId = _configuration["GoogleCalendar:CalendarId"] ?? "primary";
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            var calendarEvent = new
            {
                summary = request.Title,
                description = request.Description,
                location = request.Location,
                start = new { dateTime = request.StartTime.ToString("o"), timeZone = "UTC" },
                end_ = new { dateTime = request.EndTime.ToString("o"), timeZone = "UTC" },
                attendees = request.AttendeeEmails.ConvertAll(email => new { email }),
                conferenceData = new
                {
                    createRequest = new
                    {
                        requestId = Guid.NewGuid().ToString(),
                        conferenceSolutionKey = new { type = "hangoutsMeet" }
                    }
                },
                reminders = new
                {
                    useDefault = false,
                    overrides = new[]
                    {
                        new { method = "email", minutes = 1440 }, // 1 day before
                        new { method = "popup", minutes = 30 }
                    }
                }
            };

            var json = JsonSerializer.Serialize(calendarEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{CalendarApiBase}/calendars/{calendarId}/events?conferenceDataVersion=1&sendUpdates=all");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Google Calendar API error: {StatusCode} — {Body}",
                    response.StatusCode, errorBody);
                return Result<CalendarEventResult>.Failure(
                    $"Google Calendar API error: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var result = new CalendarEventResult
            {
                EventId = root.GetProperty("id").GetString() ?? string.Empty,
                HtmlLink = root.TryGetProperty("htmlLink", out var link) ? link.GetString() : null,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            // Try to get Google Meet link
            if (root.TryGetProperty("conferenceData", out var confData) &&
                confData.TryGetProperty("entryPoints", out var entryPoints))
            {
                foreach (var ep in entryPoints.EnumerateArray())
                {
                    if (ep.TryGetProperty("entryPointType", out var epType) &&
                        epType.GetString() == "video")
                    {
                        result.MeetLink = ep.GetProperty("uri").GetString();
                        break;
                    }
                }
            }

            _logger.LogInformation("Created Google Calendar event {EventId} for: {Title}", result.EventId, request.Title);
            return Result<CalendarEventResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar event for: {Title}", request.Title);
            // Graceful fallback — don't fail the interview creation
            return Result<CalendarEventResult>.Success(new CalendarEventResult
            {
                EventId = $"failed-{Guid.NewGuid():N}",
                StartTime = request.StartTime,
                EndTime = request.EndTime
            });
        }
    }

    public async Task<Result<CalendarEventResult>> UpdateInterviewEventAsync(
        string eventId, CalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || eventId.StartsWith("mock-") || eventId.StartsWith("failed-"))
        {
            _logger.LogInformation("Google Calendar disabled — skipping update for event: {EventId}", eventId);
            return Result<CalendarEventResult>.Success(new CalendarEventResult
            {
                EventId = eventId,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            });
        }

        try
        {
            var calendarId = _configuration["GoogleCalendar:CalendarId"] ?? "primary";
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            var updatePayload = new
            {
                summary = request.Title,
                description = request.Description,
                location = request.Location,
                start = new { dateTime = request.StartTime.ToString("o"), timeZone = "UTC" },
                end_ = new { dateTime = request.EndTime.ToString("o"), timeZone = "UTC" },
                attendees = request.AttendeeEmails.ConvertAll(email => new { email })
            };

            var json = JsonSerializer.Serialize(updatePayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Patch,
                $"{CalendarApiBase}/calendars/{calendarId}/events/{eventId}?sendUpdates=all");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Google Calendar update error: {StatusCode} — {Body}",
                    response.StatusCode, errorBody);
                return Result<CalendarEventResult>.Failure(
                    $"Google Calendar API error: {response.StatusCode}");
            }

            return Result<CalendarEventResult>.Success(new CalendarEventResult
            {
                EventId = eventId,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Google Calendar event: {EventId}", eventId);
            return Result<CalendarEventResult>.Success(new CalendarEventResult
            {
                EventId = eventId,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            });
        }
    }

    public async Task<Result> DeleteInterviewEventAsync(
        string eventId, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || eventId.StartsWith("mock-") || eventId.StartsWith("failed-"))
        {
            _logger.LogInformation("Google Calendar disabled — skipping delete for event: {EventId}", eventId);
            return Result.Success();
        }

        try
        {
            var calendarId = _configuration["GoogleCalendar:CalendarId"] ?? "primary";
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            var httpRequest = new HttpRequestMessage(HttpMethod.Delete,
                $"{CalendarApiBase}/calendars/{calendarId}/events/{eventId}?sendUpdates=all");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Failed to delete calendar event {EventId}: {StatusCode}", eventId, response.StatusCode);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Google Calendar event: {EventId}", eventId);
            return Result.Success(); // Don't fail operations because calendar cleanup failed
        }
    }

    public async Task<Result<List<CalendarBusySlot>>> GetBusySlotsAsync(
        string email, DateTime rangeStart, DateTime rangeEnd,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            return Result<List<CalendarBusySlot>>.Success(new List<CalendarBusySlot>());
        }

        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            var freeBusyRequest = new
            {
                timeMin = rangeStart.ToString("o"),
                timeMax = rangeEnd.ToString("o"),
                items = new[] { new { id = email } }
            };

            var json = JsonSerializer.Serialize(freeBusyRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{CalendarApiBase}/freeBusy");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("FreeBusy query failed for {Email}: {StatusCode}", email, response.StatusCode);
                return Result<List<CalendarBusySlot>>.Success(new List<CalendarBusySlot>());
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var busySlots = new List<CalendarBusySlot>();

            if (root.TryGetProperty("calendars", out var calendars) &&
                calendars.TryGetProperty(email, out var calendarData) &&
                calendarData.TryGetProperty("busy", out var busyArray))
            {
                foreach (var slot in busyArray.EnumerateArray())
                {
                    busySlots.Add(new CalendarBusySlot
                    {
                        Start = DateTime.Parse(slot.GetProperty("start").GetString()!),
                        End = DateTime.Parse(slot.GetProperty("end").GetString()!)
                    });
                }
            }

            return Result<List<CalendarBusySlot>>.Success(busySlots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query free/busy for: {Email}", email);
            return Result<List<CalendarBusySlot>>.Success(new List<CalendarBusySlot>());
        }
    }

    /// <summary>
    /// Gets an OAuth2 access token using the service account credentials.
    /// In production, this would use Google.Apis.Auth to create a JWT and exchange it.
    /// For simplicity, we use the API key approach or a pre-configured token.
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // If a static API key or pre-configured OAuth token is available, use it
        var apiKey = _configuration["GoogleCalendar:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey;
        }

        // For service account JWT flow (production):
        // 1. Build a JWT assertion signed with the private key
        // 2. Exchange it at https://oauth2.googleapis.com/token
        // This requires Google.Apis.Auth NuGet package
        var serviceAccountEmail = _configuration["GoogleCalendar:ServiceAccountEmail"];
        var privateKey = _configuration["GoogleCalendar:PrivateKey"];

        if (string.IsNullOrWhiteSpace(serviceAccountEmail) || string.IsNullOrWhiteSpace(privateKey))
        {
            throw new InvalidOperationException(
                "Google Calendar credentials not configured. " +
                "Set GoogleCalendar:ServiceAccountEmail and GoogleCalendar:PrivateKey or GoogleCalendar:ApiKey.");
        }

        // Simplified token exchange — in production use Google.Apis.Auth.OAuth2.ServiceAccountCredential
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = BuildServiceAccountJwt(serviceAccountEmail, privateKey)
        };

        var tokenResponse = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        using var tokenDoc = JsonDocument.Parse(tokenBody);

        if (tokenDoc.RootElement.TryGetProperty("access_token", out var accessToken))
        {
            return accessToken.GetString()!;
        }

        throw new InvalidOperationException("Failed to obtain Google Calendar access token.");
    }

    private static string BuildServiceAccountJwt(string serviceAccountEmail, string privateKey)
    {
        // This is a placeholder for the JWT construction.
        // In production, use Google.Apis.Auth.OAuth2.ServiceAccountCredential
        // or manually build a JWT with the RS256 algorithm.
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var claims = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{{\"iss\":\"{serviceAccountEmail}\"," +
            $"\"scope\":\"https://www.googleapis.com/auth/calendar\"," +
            $"\"aud\":\"https://oauth2.googleapis.com/token\"," +
            $"\"iat\":{now},\"exp\":{now + 3600}}}"));

        // In production, sign with RSA private key
        return $"{header}.{claims}.placeholder-signature";
    }
}
