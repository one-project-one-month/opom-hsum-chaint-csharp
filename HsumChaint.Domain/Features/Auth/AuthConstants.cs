namespace HsumChaint.Domain.Features.Auth
{
    public static class LoginModeCodes
    {
        public const string PhoneOnly = "PHONE_ONLY";
        public const string EmailOnly = "EMAIL_ONLY";
        public const string PhoneOrEmail = "PHONE_OR_EMAIL";
    }

    public static class OtpChannels
    {
        public const string Email = "EMAIL";
        public const string Phone = "PHONE";
    }

    public static class OtpPurposes
    {
        public const string VerifyEmail = "VERIFY_EMAIL";
        public const string VerifyPhone = "VERIFY_PHONE";
        public const string Login = "LOGIN";
        public const string ResetPassword = "RESET_PASSWORD";
        public const string ChangePassword = "CHANGE_PASSWORD";
    }
}
