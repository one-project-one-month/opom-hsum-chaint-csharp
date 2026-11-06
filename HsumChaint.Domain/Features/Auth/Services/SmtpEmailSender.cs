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
        public async Task SendOtpAsync(string email, string otp, DateTime expiresAt)
        {
            using var message = new MailMessage(new MailAddress(_options.FromEmail, _options.FromName), new MailAddress(email))
            {
                Subject = "Your HsumChaint verification code",
                Body = $"Your OTP is {otp}. It expires at {expiresAt:O}. Do not share this code. If you did not request it, ignore this email."
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
