namespace ServiceHub.Web.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ServiceHub";

    public string Audience { get; set; } = "ServiceHub";

    public int ExpiryMinutes { get; set; } = 60;

    public string Key { get; set; } = string.Empty;
}
