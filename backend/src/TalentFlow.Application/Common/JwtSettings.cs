namespace TalentFlow.Application.Common;

/// <summary>
/// JWT configuration settings bound from appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
