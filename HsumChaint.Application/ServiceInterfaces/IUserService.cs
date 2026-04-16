using HsumChaint.Application.DTOs.User;
using HsumChaint.Infrastructure.Models;

namespace HsumChaint.Application.ServiceInterfaces
{
    public interface IUserService
    {
        Task<ApplicationCommonResponseModel<List<UserDto>>> GetAllUsers();

        Task<ApplicationCommonResponseModel<UserDto>> GetUser(int id);

        Task<ApplicationCommonResponseModel<UserDto>> PutUser(UserDto user);

        Task<ApplicationCommonResponseModel<UserDto>> DeleteUser(int id);

        Task<ApplicationCommonResponseModel<List<InvitationDto>>> GetUserInvitationList(int id);

        Task<ApplicationCommonResponseModel<List<InvitationDto>>> GetInvitedByOtherList(int id);

        Task<ApplicationCommonResponseModel<List<NotificationDto>>> GetUserNotificationList(int id);

        Task<ApplicationCommonResponseModel<List<NotificationDto>>> DeleteUserNotificationList(int id);
    }
}