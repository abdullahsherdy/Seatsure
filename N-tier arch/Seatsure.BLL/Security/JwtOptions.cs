namespace Seatsure.BLL.Security;

/// <summary>Bound from the "Jwt" configuration section. See appsettings.json.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    /// <summary>Signing key. Must be at least 32 bytes for HMAC-SHA256. Keep out of source control in production.</summary>
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
