using System.Threading.Tasks;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Shared;

namespace HsumChaint.Domain.Features.Auth.ServiceInterfaces
{
    public interface IOtpService
    {
        Task<Result<OtpResponseDto>> Request(RequestOtpDto request);

        Task<Result> Verify(VerifyOtpDto request);
    }
}
