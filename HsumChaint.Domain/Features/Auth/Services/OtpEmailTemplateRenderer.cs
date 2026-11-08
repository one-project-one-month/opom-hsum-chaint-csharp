using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace HsumChaint.Domain.Features.Auth.Services;

public sealed class OtpEmailTemplateRenderer
{
    private const string TemplateResourceSuffix = ".Features.Auth.EmailTemplates.OtpEmailTemplate.html";
    private const string OtpCodePlaceholder = "{{OTP_CODE}}";
    private const string OtpExpiryPlaceholder = "{{OTP_EXPIRY_MINUTES}}";
    private const string CurrentYearPlaceholder = "{{CURRENT_YEAR}}";

    private readonly string _template;

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
        _template = reader.ReadToEnd();

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
