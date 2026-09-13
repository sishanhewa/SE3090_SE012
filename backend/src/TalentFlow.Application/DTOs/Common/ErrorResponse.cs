namespace TalentFlow.Application.DTOs.Common;

/// <summary>
/// Standardized API error response format.
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public IDictionary<string, string[]>? ValidationErrors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
