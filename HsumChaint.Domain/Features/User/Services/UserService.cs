using AutoMapper;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.User.DTOs;
using HsumChaint.Domain.Features.User.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = HsumChaint.Database.Models.Notification;
using UserEntity = HsumChaint.Database.Models.User;

namespace HsumChaint.Domain.Features.User.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public UserService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region GetUserList
        public async Task<ApplicationCommonResponseModel<List<UserDto>>> GetAllUsers()
        {
            var response = new ApplicationCommonResponseModel<List<UserDto>>();
            try
            {
                List<UserEntity> userList = await _context.Users
                    .AsNoTracking()
                    .Where(user => user.IsDeleted == false)
                    .ToListAsync();

                response.ListData = _mapper.Map<List<UserDto>>(userList);
                response.IsSuccess = true;
                response.Message = userList.Count > 0 ? "Successfully Retrieved User Lists" : "User list not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }
        #endregion

        #region GetUserById
        public async Task<ApplicationCommonResponseModel<UserDto>> GetUser(int id)
        {
            var response = new ApplicationCommonResponseModel<UserDto>();
            try
            {
                UserEntity? user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user => user.IsDeleted == false && user.Id == id);

                response.Data = _mapper.Map<UserDto>(user);
                response.IsSuccess = true;
                response.Message = user is not null ? "Successfully Retrieved User Lists" : "User not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }
        #endregion

        #region UpdateUser
        public async Task<ApplicationCommonResponseModel<UserDto>> PutUser(UserDto user)
        {
            var response = new ApplicationCommonResponseModel<UserDto>();
            try
            {
                var userEntity = _mapper.Map<UserEntity>(user);
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(dbUser => dbUser.IsDeleted == false && dbUser.Id == userEntity.Id);

                if (existingUser is not null)
                {
                    string errorMessage;
                    bool userValidation = ValidateForUserUpdate(userEntity, out errorMessage);

                    if (!userValidation)
                    {
                        response.Data = null;
                        response.IsSuccess = false;
                        response.Message = $"User data validation failed: {errorMessage}";
                        return response;
                    }

                    existingUser.Name = userEntity.Name;
                    existingUser.PhoneNumber = userEntity.PhoneNumber;
                    existingUser.UserType = userEntity.UserType;
                    existingUser.Email = userEntity.Email;
                    existingUser.ContactPhoneNumber = userEntity.ContactPhoneNumber;

                    _context.Users.Update(existingUser);
                    var result = await _context.SaveChangesAsync();

                    if (result <= 0)
                    {
                        response.Data = null;
                        response.IsSuccess = false;
                        response.Message = "Failed to update user data.";
                        return response;
                    }

                    response.Data = null;
                    response.IsSuccess = true;
                    response.Message = "User updated successfully.";
                }
                else
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.Message = "User not found";
                }
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }

            return response;
        }
        #endregion

        #region DeleteUser
        public async Task<ApplicationCommonResponseModel<UserDto>> DeleteUser(int id)
        {
            var response = new ApplicationCommonResponseModel<UserDto>();
            try
            {
                UserEntity? user = await _context.Users.FindAsync(id);

                if (user is not null)
                {
                    user.IsDeleted = true;
                    user.UpdatedAt = DateTime.UtcNow;

                    var result = await _context.SaveChangesAsync();

                    if (result <= 0)
                    {
                        response.IsSuccess = false;
                        response.Message = "Failed to delete user data.";
                        return response;
                    }

                    response.IsSuccess = true;
                    response.Message = "User deleted successfully.";
                }
                else
                {
                    response.IsSuccess = true;
                    response.Message = "User not found";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }
        #endregion

        #region Invitation
        public async Task<ApplicationCommonResponseModel<List<InvitationDto>>> GetUserInvitationList(int id)
        {
            var response = new ApplicationCommonResponseModel<List<InvitationDto>>();
            try
            {
                List<Invitation> invitationList = await _context.Invitations
                    .AsNoTracking()
                    .Where(invitation => invitation.InvitedUserId == id)
                    .ToListAsync();

                response.ListData = _mapper.Map<List<InvitationDto>>(invitationList);
                response.IsSuccess = true;
                response.Message = invitationList.Count > 0 ? "Successfully Retrieved Invitation Lists" : "Invitation list not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }

        public async Task<ApplicationCommonResponseModel<List<InvitationDto>>> GetInvitedByOtherList(int id)
        {
            var response = new ApplicationCommonResponseModel<List<InvitationDto>>();
            try
            {
                List<Invitation> invitedByOtherList = await _context.Invitations
                    .AsNoTracking()
                    .Where(invitation => invitation.InvitedById == id)
                    .ToListAsync();

                response.ListData = _mapper.Map<List<InvitationDto>>(invitedByOtherList);
                response.IsSuccess = true;
                response.Message = invitedByOtherList.Count > 0 ? "Successfully Retrieved Invited Lists" : "Invited list not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }
        #endregion

        #region Notification
        public async Task<ApplicationCommonResponseModel<List<NotificationDto>>> GetUserNotificationList(int id)
        {
            var response = new ApplicationCommonResponseModel<List<NotificationDto>>();
            try
            {
                List<NotificationEntity> notificationList = await _context.Notifications
                    .AsNoTracking()
                    .Where(notification => notification.UserId == id && notification.IsDelete == false)
                    .ToListAsync();

                response.ListData = _mapper.Map<List<NotificationDto>>(notificationList);
                response.IsSuccess = true;
                response.Message = notificationList.Count > 0
                    ? "Successfully Retrieved User's Notification Lists"
                    : "User's Notification list not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }

        public async Task<ApplicationCommonResponseModel<List<NotificationDto>>> DeleteUserNotificationList(int id)
        {
            var response = new ApplicationCommonResponseModel<List<NotificationDto>>();
            try
            {
                List<NotificationEntity> notificationList = await _context.Notifications
                    .Where(notification => notification.UserId == id && notification.IsDelete == false)
                    .ToListAsync();

                if (notificationList.Count > 0)
                {
                    foreach (NotificationEntity notification in notificationList)
                    {
                        notification.IsDelete = true;
                    }

                    var result = await _context.SaveChangesAsync();

                    if (result <= 0)
                    {
                        response.IsSuccess = false;
                        response.Message = "Failed to delete user data.";
                        return response;
                    }

                    response.IsSuccess = true;
                    response.Message = "User deleted successfully.";
                }
                else
                {
                    response.IsSuccess = true;
                    response.Message = "User's Notification List not found";
                }

                response.Data = null;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }
        #endregion

        private static bool ValidateForUserUpdate(UserEntity? user, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (user is null)
            {
                errorMessage = "User cannot be null.";
                return false;
            }

            if (string.IsNullOrEmpty(user.Name))
            {
                errorMessage = "User name cannot be null or empty.";
                return false;
            }

            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                errorMessage = "User phone number cannot be null or empty.";
                return false;
            }

            return true;
        }
    }
}
