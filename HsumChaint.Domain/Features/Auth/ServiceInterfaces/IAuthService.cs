using System.Threading.Tasks;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Shared;

namespace HsumChaint.Domain.Features.Auth.ServiceInterfaces
{
    public interface IAuthService
    {
        Task<Result<LoginResponseDto>> Login(LoginRequestDto reqModel);
        Task<Result> Register(RegisterRequestDto reqModel);
        Task<Result<LoginResponseDto>> RefreshTokens(RefreshTokenRequestDto request);
        Task<Result> ResetPassword(ResetPasswordRequestDto request);
        Task<Result> ChangePassword(int userId, ChangePasswordRequestDto request);
    }
}
