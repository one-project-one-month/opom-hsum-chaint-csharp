using System.Collections.Generic;
using System.Threading.Tasks;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Shared;

namespace HsumChaint.Domain.Features.Auth.ServiceInterfaces
{
    public interface ILoginModeService
    {
        Task<Result<LoginMode>> GetActiveMode();

        Task<Result<IReadOnlyList<LoginModeDto>>> GetAllModes();

        Task<Result> UpdateActiveMode(int adminUserId, UpdateLoginModeRequestDto request);
    }
}
