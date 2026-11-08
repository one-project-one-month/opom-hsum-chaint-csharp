using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using HsumChaint.Domain.Configuration;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using Microsoft.Extensions.Options;

namespace HsumChaint.Domain.Features.Auth.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly OtpOptions _otpOptions;
        private readonly OtpEmailTemplateRenderer _templateRenderer;

        public SmtpEmailSender(
            IOptions<SmtpOptions> options,
            IOptions<OtpOptions> otpOptions,
            OtpEmailTemplateRenderer templateRenderer)
        {
            _options = options.Value;
            _otpOptions = otpOptions.Value;
            _templateRenderer = templateRenderer;
        }

        #region Send OTP
        public async Task SendOtpAsync(string email, string otp, DateTime expiresAt, string? purpose = null)
        {
            var subject = purpose switch
            {
                OtpPurposes.ResetPassword => "Reset your HsumChaint password",
                OtpPurposes.ChangePassword => "Change your HsumChaint password",
                OtpPurposes.VerifyEmail => "Verify your HsumChaint email",
                _ => "Your HsumChaint verification code"
            };

            var body = _templateRenderer.Render(otp, _otpOptions.LifetimeMinutes, DateTime.UtcNow.Year);

            using var message = new MailMessage(new MailAddress(_options.FromEmail, _options.FromName), new MailAddress(email))
            {
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl
            };

            if (!string.IsNullOrEmpty(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            await client.SendMailAsync(message);
        }
        #endregion
    }
}
