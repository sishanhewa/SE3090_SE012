using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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
/// Uses either an organizer OAuth refresh token or Workspace service account delegation.
/// Reports configuration and API failures explicitly so invitations are never mistaken for sent.
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

        var workspaceCredentials = !string.IsNullOrWhiteSpace(_configuration["GoogleCalendar:ServiceAccountEmail"])
            && !string.IsNullOrWhiteSpace(_configuration["GoogleCalendar:PrivateKey"])
            && !string.IsNullOrWhiteSpace(_configuration["GoogleCalendar:DelegatedUserEmail"]);
        var organizerCredentials = !string.IsNullOrWhiteSpace(_configuration["GoogleCalendar:OAuthClientId"])
            && !string.IsNullOrWhiteSpace(_configuration["GoogleCalendar:OAuthClientSecret"])
            && !string.IsNullOrWhiteSpace(_configuration["GoogleCalendar:OAuthRefreshToken"]);
        _isEnabled = !string.IsNullOrWhiteSpace(_configuration["GoogleCalendar:CalendarId"])
            && (workspaceCredentials || organizerCredentials);

        if (!_isEnabled)
        {
            _logger.LogWarning(
                "Google Calendar integration is disabled. " +
                "Set a Calendar ID and organizer OAuth credentials, or Workspace service account delegation, to enable invitations.");
        }
    }

    public async Task<Result<CalendarEventResult>> CreateInterviewEventAsync(
        CalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            return Result<CalendarEventResult>.Failure("Google Calendar is not configured.");
        }

        try
        {
            var calendarId = _configuration["GoogleCalendar:CalendarId"] ?? "primary";
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            var calendarEvent = new Dictionary<string, object?>
            {
                ["id"] = request.EventId,
                ["summary"] = request.Title,
                ["description"] = request.Description,
                ["location"] = request.Location,
                ["start"] = new { dateTime = request.StartTime.ToUniversalTime().ToString("o"), timeZone = "UTC" },
                ["end"] = new { dateTime = request.EndTime.ToUniversalTime().ToString("o"), timeZone = "UTC" },
                ["attendees"] = request.AttendeeEmails.ConvertAll(email => new { email }),
                ["conferenceData"] = new
                {
                    createRequest = new
                    {
                        requestId = Guid.NewGuid().ToString(),
                        conferenceSolutionKey = new { type = "hangoutsMeet" }
                    }
                },
                ["reminders"] = new
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

            // A client-chosen event ID makes retries safe when Google created the event
            // but the original response was lost on the network.
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict &&
                !string.IsNullOrWhiteSpace(request.EventId))
            {
                using var existingRequest = new HttpRequestMessage(HttpMethod.Get,
                    $"{CalendarApiBase}/calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(request.EventId)}");
                existingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                response = await _httpClient.SendAsync(existingRequest, cancellationToken);
            }

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
            if (root.TryGetProperty("start", out var actualStart) &&
                actualStart.TryGetProperty("dateTime", out var actualStartValue) &&
                DateTimeOffset.TryParse(actualStartValue.GetString(), out var actualStartTime) &&
                Math.Abs((actualStartTime.UtcDateTime - request.StartTime.ToUniversalTime()).TotalMinutes) > 1)
                return Result<CalendarEventResult>.Failure("An existing Calendar event has a different interview time.");
            if (!root.TryGetProperty("attendees", out var actualAttendees) ||
                request.AttendeeEmails.Any(email => !actualAttendees.EnumerateArray().Any(attendee =>
                    attendee.TryGetProperty("email", out var address) &&
                    string.Equals(address.GetString(), email, StringComparison.OrdinalIgnoreCase))))
                return Result<CalendarEventResult>.Failure("Google Calendar did not confirm the candidate invitation.");

            var result = new CalendarEventResult
            {
                EventId = root.GetProperty("id").GetString() ?? string.Empty,
                HtmlLink = root.TryGetProperty("htmlLink", out var link) ? link.GetString() : null,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };
            if (string.IsNullOrWhiteSpace(result.EventId))
                return Result<CalendarEventResult>.Failure("Google Calendar did not return an event ID.");

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

            _logger.LogInformation("Confirmed Google Calendar event {EventId} for: {Title}", result.EventId, request.Title);
            return Result<CalendarEventResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar event for: {Title}", request.Title);
            return Result<CalendarEventResult>.Failure("Google Calendar invitation could not be created.");
        }
    }

    public async Task<Result<CalendarEventResult>> UpdateInterviewEventAsync(
        string eventId, CalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || eventId.StartsWith("mock-") || eventId.StartsWith("failed-"))
        {
            return Result<CalendarEventResult>.Failure("Google Calendar is not configured for this event.");
        }

        try
        {
            var calendarId = _configuration["GoogleCalendar:CalendarId"] ?? "primary";
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            var updatePayload = new Dictionary<string, object?>
            {
                ["summary"] = request.Title,
                ["description"] = request.Description,
                ["location"] = request.Location,
                ["start"] = new { dateTime = request.StartTime.ToUniversalTime().ToString("o"), timeZone = "UTC" },
                ["end"] = new { dateTime = request.EndTime.ToUniversalTime().ToString("o"), timeZone = "UTC" },
                ["attendees"] = request.AttendeeEmails.ConvertAll(email => new { email })
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
            return Result<CalendarEventResult>.Failure("Google Calendar invitation could not be updated.");
        }
    }

    public async Task<Result> DeleteInterviewEventAsync(
        string eventId, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || eventId.StartsWith("mock-") || eventId.StartsWith("failed-"))
        {
            return eventId.StartsWith("mock-") || eventId.StartsWith("failed-")
                ? Result.Success() : Result.Failure("Google Calendar is not configured for this event.");
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
                return Result.Failure("Google Calendar cancellation could not be sent.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Google Calendar event: {EventId}", eventId);
            return Result.Failure("Google Calendar cancellation could not be sent.");
        }
    }

    public async Task<Result<List<CalendarBusySlot>>> GetBusySlotsAsync(
        string email, DateTime rangeStart, DateTime rangeEnd,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            return Result<List<CalendarBusySlot>>.Failure("Google Calendar availability is not configured.");
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
                return Result<List<CalendarBusySlot>>.Failure("Google Calendar availability could not be checked.");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var busySlots = new List<CalendarBusySlot>();

            if (root.TryGetProperty("calendars", out var calendars) &&
                calendars.TryGetProperty(email, out var calendarData) &&
                calendarData.TryGetProperty("busy", out var busyArray))
            {
                if (calendarData.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                    return Result<List<CalendarBusySlot>>.Failure("Google Calendar did not return reliable availability.");
                foreach (var slot in busyArray.EnumerateArray())
                {
                    busySlots.Add(new CalendarBusySlot
                    {
                        Start = DateTimeOffset.Parse(slot.GetProperty("start").GetString()!).UtcDateTime,
                        End = DateTimeOffset.Parse(slot.GetProperty("end").GetString()!).UtcDateTime
                    });
                }
            }

            else return Result<List<CalendarBusySlot>>.Failure("Google Calendar did not return availability for this calendar.");
            return Result<List<CalendarBusySlot>>.Success(busySlots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query free/busy for: {Email}", email);
            return Result<List<CalendarBusySlot>>.Failure("Google Calendar availability could not be checked.");
        }
    }

    /// <summary>
    /// Gets a short-lived access token using a server-held organizer refresh token or
    /// a delegated Workspace service account. No long-lived secret is sent to clients.
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var oauthClientId = _configuration["GoogleCalendar:OAuthClientId"];
        var oauthClientSecret = _configuration["GoogleCalendar:OAuthClientSecret"];
        var oauthRefreshToken = _configuration["GoogleCalendar:OAuthRefreshToken"];
        if (!string.IsNullOrWhiteSpace(oauthClientId) && !string.IsNullOrWhiteSpace(oauthClientSecret)
            && !string.IsNullOrWhiteSpace(oauthRefreshToken))
        {
            using var refreshResponse = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = oauthClientId,
                    ["client_secret"] = oauthClientSecret,
                    ["refresh_token"] = oauthRefreshToken,
                    ["grant_type"] = "refresh_token"
                }), cancellationToken);
            refreshResponse.EnsureSuccessStatusCode();
            using var refreshDocument = JsonDocument.Parse(
                await refreshResponse.Content.ReadAsStringAsync(cancellationToken));
            if (refreshDocument.RootElement.TryGetProperty("access_token", out var refreshedToken)
                && !string.IsNullOrWhiteSpace(refreshedToken.GetString()))
                return refreshedToken.GetString()!;
            throw new InvalidOperationException("Google OAuth did not return an access token.");
        }

        var serviceAccountEmail = _configuration["GoogleCalendar:ServiceAccountEmail"];
        var privateKey = _configuration["GoogleCalendar:PrivateKey"];

        if (string.IsNullOrWhiteSpace(serviceAccountEmail) || string.IsNullOrWhiteSpace(privateKey))
        {
            throw new InvalidOperationException(
                "Google Calendar credentials not configured. " +
                "Set organizer OAuth credentials or Workspace service account delegation.");
        }

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = BuildServiceAccountJwt(serviceAccountEmail, privateKey)
        };

        var tokenResponse = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        tokenResponse.EnsureSuccessStatusCode();
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        using var tokenDoc = JsonDocument.Parse(tokenBody);

        if (tokenDoc.RootElement.TryGetProperty("access_token", out var accessToken))
        {
            return accessToken.GetString()!;
        }

        throw new InvalidOperationException("Failed to obtain Google Calendar access token.");
    }

    private string BuildServiceAccountJwt(string serviceAccountEmail, string privateKey)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var claims = new Dictionary<string, object>
        {
            ["iss"] = serviceAccountEmail,
            ["scope"] = "https://www.googleapis.com/auth/calendar.events https://www.googleapis.com/auth/calendar.events.freebusy",
            ["aud"] = "https://oauth2.googleapis.com/token",
            ["iat"] = now,
            ["exp"] = now + 3600
        };
        var delegatedUser = _configuration["GoogleCalendar:DelegatedUserEmail"];
        if (!string.IsNullOrWhiteSpace(delegatedUser)) claims["sub"] = delegatedUser;
        static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var body = Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(claims)));
        var unsigned = $"{header}.{body}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey.Replace("\\n", "\n"));
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{unsigned}.{Encode(signature)}";
    }
}
