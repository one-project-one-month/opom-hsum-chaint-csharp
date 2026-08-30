using HsumChaint.Domain.Features.Auth.DTOs;

namespace HsumChaint.Domain.Features.Auth.ServiceInterfaces
{
    public interface IAuthService
    {
        Task<ApplicationCommonResponseModel<LoginResponseDto>> Login(LoginRequestDto reqModel);
        Task<ApplicationCommonResponseModel<RegisterResponseDto>> Register(RegisterRequestDto reqModel);

        Task<ApplicationCommonResponseModel<LoginResponseDto>> RefreshTokens(RefreshTokenRequestDto request);
    }
}




