namespace HsumChaint.Domain.Configuration
{
    public class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public bool UseSsl { get; set; } = true;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = "HsumChaint";
    }

    public class OtpOptions
    {
        public int LifetimeMinutes { get; set; } = 5;

        public int MaxAttempts { get; set; } = 5;

        public int ResendCooldownSeconds { get; set; } = 60;
    }
}
