using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HsumChaint.Domain.Features.Auth.Services;

public sealed class OtpEmailTemplateRenderer
{
    private const string TemplateResourceSuffix = ".Features.Auth.EmailTemplates.OtpEmailTemplate.html";
    private const string OtpCodePlaceholder = "{{OTP_CODE}}";
    private const string OtpExpiryPlaceholder = "{{OTP_EXPIRY_MINUTES}}";
    private const string CurrentYearPlaceholder = "{{CURRENT_YEAR}}";
    public const string LogoContentId = "hsumchaint-logo";

    private static readonly Regex EmbeddedLogoRegex = new(
        "data:image/png;base64,(?<data>[A-Za-z0-9+/=]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _template;
    public byte[] LogoBytes { get; }

    public OtpEmailTemplateRenderer()
    {
        var assembly = typeof(OtpEmailTemplateRenderer).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(TemplateResourceSuffix, StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException("The OTP email template embedded resource was not found.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The OTP email template embedded resource could not be opened.");
        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();
        var logoMatch = EmbeddedLogoRegex.Match(template);
        if (!logoMatch.Success)
        {
            throw new InvalidOperationException("The OTP email template is missing its embedded PNG logo.");
        }

        LogoBytes = Convert.FromBase64String(logoMatch.Groups["data"].Value);
        _template = EmbeddedLogoRegex.Replace(template, $"cid:{LogoContentId}", 1);

        if (!_template.Contains(OtpCodePlaceholder, StringComparison.Ordinal)
            || !_template.Contains(OtpExpiryPlaceholder, StringComparison.Ordinal)
            || !_template.Contains(CurrentYearPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The OTP email template is missing one or more required placeholders.");
        }
    }

    public string Render(string otpCode, int expiryMinutes, int currentYear)
    {
        return _template
            .Replace(OtpCodePlaceholder, WebUtility.HtmlEncode(otpCode), StringComparison.Ordinal)
            .Replace(OtpExpiryPlaceholder, WebUtility.HtmlEncode(expiryMinutes.ToString()), StringComparison.Ordinal)
            .Replace(CurrentYearPlaceholder, WebUtility.HtmlEncode(currentYear.ToString()), StringComparison.Ordinal);
    }
}
