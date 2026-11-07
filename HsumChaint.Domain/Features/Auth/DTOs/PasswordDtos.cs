namespace HsumChaint.Domain.Features.Auth.DTOs;

public record ResetPasswordRequestDto(string Email, string Otp, string NewPassword);

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword, string Otp);
