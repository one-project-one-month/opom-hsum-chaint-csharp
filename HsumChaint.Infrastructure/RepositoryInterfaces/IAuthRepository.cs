using HsumChaint.Infrastructure.Models;

namespace HsumChaint.Infrastructure.RepositoryInterfaces
{
    public interface IAuthRepository
    {
        Task<CommonResponseModel<User>> GetUserByPhoneNumber(string phoneNumber);
        Task<CommonResponseModel<User>> Register(User requestModel);
        Task<CommonResponseModel<MonkProfile>> RegisterMonkProfile(MonkProfile requestModel);
        Task<CommonResponseModel<RefreshToken>> AddOrUpdateRefreshToken(RefreshToken reqModel);
        Task<CommonResponseModel<RefreshToken>> GetRefreshTokenByUserId(int userId);

        Task<CommonResponseModel<User>> GetUserById(int userId);
    }
}