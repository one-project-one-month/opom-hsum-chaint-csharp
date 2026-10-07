using HsumChaint.Shared;
using HsumChaint.Domain.Features.Auth.DTOs;

namespace HsumChaint.Domain.Features.Auth.ServiceInterfaces
{
    public interface IAuthService
    {
        Task<Result<LoginResponseDto>> Login(LoginRequestDto reqModel);
        Task<Result> Register(RegisterRequestDto reqModel);

        Task<Result<LoginResponseDto>> RefreshTokens(RefreshTokenRequestDto request);
    }
}




