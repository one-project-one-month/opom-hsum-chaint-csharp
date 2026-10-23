using HsumChaint.Shared;
using HsumChaint.Domain.Features.User.DTOs;

namespace HsumChaint.Domain.Features.User.ServiceInterfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserDto>> GetAllUsers(PaginationRequest? pagination = null);
        Task<Result<UserDto>> GetUser(GetUserRequestDto request);
        Task<Result> PutUser(UserDto user);
        Task<Result> DeleteUser(DeleteUserRequestDto request);
        Task<PagedResult<InvitationDto>> GetUserInvitationList(GetUserInvitationsRequestDto request);
        Task<PagedResult<InvitationDto>> GetInvitedByOtherList(GetInvitedByOtherRequestDto request);
        Task<PagedResult<NotificationDto>> GetUserNotificationList(GetUserNotificationsRequestDto request);
        Task<Result> DeleteUserNotificationList(DeleteUserNotificationsRequestDto request);
        Task<Result<MonkProfileDto>> GetMonkProfile(int currentUserId, GetMonkProfileRequestDto request);
        Task<Result<MonkProfileDto>> UpdateMonkProfile(int currentUserId, UpdateMonkProfileRequestDto request);
        Task<Result> DeleteMonkProfile(int currentUserId, DeleteMonkProfileRequestDto request);
        Task<Result<UserSettingDto>> GetUserSettings(int currentUserId, GetUserSettingsRequestDto request);
        Task<Result<UserSettingDto>> UpdateUserSettings(int currentUserId, UpdateUserSettingRequestDto request);
    }
}
