namespace HsumChaint.Shared.Configuration;

public class JwtOptions
{
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? Key { get; set; }
    public int? TokenValidityMins { get; set; }
}
