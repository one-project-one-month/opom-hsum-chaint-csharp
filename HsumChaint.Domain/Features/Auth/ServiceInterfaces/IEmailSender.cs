using System;
using System.Threading.Tasks;

namespace HsumChaint.Domain.Features.Auth.ServiceInterfaces
{
    public interface IEmailSender
    {
        Task SendOtpAsync(string email, string otp, DateTime expiresAt, string? purpose = null);
    }
}
