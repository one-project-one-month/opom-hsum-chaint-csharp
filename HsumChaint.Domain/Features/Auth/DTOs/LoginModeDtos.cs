using System;

namespace HsumChaint.Domain.Features.Auth.DTOs
{
    public class LoginModeDto
    {
        public string Code { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public bool RequirePhoneVerification { get; set; }

        public bool RequireEmailVerification { get; set; }

        public LoginModeDto()
        {
        }

        public LoginModeDto(string code, string displayName, bool isActive, bool requirePhoneVerification, bool requireEmailVerification)
        {
            Code = code;
            DisplayName = displayName;
            IsActive = isActive;
            RequirePhoneVerification = requirePhoneVerification;
            RequireEmailVerification = requireEmailVerification;
        }
    }

    public class UpdateLoginModeRequestDto
    {
        public string? Code { get; set; }

        public bool RequirePhoneVerification { get; set; }

        public bool RequireEmailVerification { get; set; }
    }

    public class PublicLoginConfigurationDto
    {
        public string Code { get; set; } = string.Empty;

        public bool RequirePhoneVerification { get; set; }

        public bool RequireEmailVerification { get; set; }

        public PublicLoginConfigurationDto()
        {
        }

        public PublicLoginConfigurationDto(string code, bool requirePhoneVerification, bool requireEmailVerification)
        {
            Code = code;
            RequirePhoneVerification = requirePhoneVerification;
            RequireEmailVerification = requireEmailVerification;
        }
    }

    public class RequestOtpDto
    {
        public string? Destination { get; set; }

        public string? Channel { get; set; }

        public string? Purpose { get; set; }
    }

    public class VerifyOtpDto : RequestOtpDto
    {
        public string? Code { get; set; }
    }

    public class OtpResponseDto
    {
        public DateTime ExpiresAt { get; set; }

        public int ResendAfterSeconds { get; set; }

        public OtpResponseDto()
        {
        }

        public OtpResponseDto(DateTime expiresAt, int resendAfterSeconds)
        {
            ExpiresAt = expiresAt;
            ResendAfterSeconds = resendAfterSeconds;
        }
    }
}
