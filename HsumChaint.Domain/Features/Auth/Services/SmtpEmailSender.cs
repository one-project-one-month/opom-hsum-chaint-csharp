using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using HsumChaint.Domain.Configuration;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using Microsoft.Extensions.Options;

namespace HsumChaint.Domain.Features.Auth.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;

        public SmtpEmailSender(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
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

            var body = $"Your verification code is: {otp}\n\nThis code will expire in 5 minutes.\nDo not share this code with anyone. If you did not request it, you can safely ignore this email.";

            using var message = new MailMessage(new MailAddress(_options.FromEmail, _options.FromName), new MailAddress(email))
            {
                Subject = subject,
                Body = body
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
