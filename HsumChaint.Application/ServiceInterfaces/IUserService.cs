using HsumChaint.Application.DTOs.User;

namespace HsumChaint.Application.ServiceInterfaces
{
    public interface IUserService
    {
        Task<ApplicationCommonResponseModel<UserDto>> AddUser(UserDto reqModel);

        Task<ApplicationCommonResponseModel<List<UserDto>>> GetAllUsers();

        Task<ApplicationCommonResponseModel<UserDto>> GetUser(int id);
    }
}