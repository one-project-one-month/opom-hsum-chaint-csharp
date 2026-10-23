using HsumChaint.Shared;
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

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        #region GetUserList
        public async Task<PagedResult<UserDto>> GetAllUsers(PaginationRequest? pagination = null)
        {
            var pageNumber = pagination?.PageNumber ?? 1;
            var pageSize = pagination?.PageSize ?? 10;
            if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<UserDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            try
            {
                var query = _context.Users.Include(u => u.Role).AsNoTracking().Where(u => u.IsDeleted == false);
                var totalCount = await query.CountAsync();
                var items = await query.OrderBy(x => x.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var data = items.Select(MapToDto).ToList();
                return PagedResult<UserDto>.Success(data, new Pagination(pageNumber, pageSize, totalCount), "Successfully Retrieved User Lists");
            }
            catch (Exception ex)
            {
                return PagedResult<UserDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }
        #endregion

        #region GetUserById
        public async Task<Result<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users.Include(u => u.Role).AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IsDeleted == false && u.Id == id);
                return user == null
                    ? Result<UserDto>.Failure("User not found")
                    : Result<UserDto>.Success(MapToDto(user), "Successfully Retrieved User");
            }
            catch (Exception ex)
            {
                return Result<UserDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }
        #endregion

        #region UpdateUser
        public async Task<Result> PutUser(UserDto user)
        {
            try
            {
                var userEntity = MapToEntity(user);
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.IsDeleted == false && u.Id == user.Id);
                if (existingUser == null) return Result.Failure("User not found");
                if (!ValidateForUserUpdate(userEntity, out var errorMessage))
                    return Result.Failure($"User data validation failed: {errorMessage}");
                if (!await _context.Roles.AnyAsync(r => r.Id == user.RoleId && !r.IsDeleted))
                    return Result.Failure("Role not found.");

                existingUser.Name = userEntity.Name;
                existingUser.PhoneNumber = userEntity.PhoneNumber;
                existingUser.RoleId = userEntity.RoleId;
                existingUser.Email = userEntity.Email;
                existingUser.ContactPhoneNumber = userEntity.ContactPhoneNumber;
                _context.Users.Update(existingUser);
                return await _context.SaveChangesAsync() > 0
                    ? Result.Success("User updated successfully.")
                    : Result.Failure("Failed to update user data.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application Layer Exception: {ex.Message}");
            }
        }
        #endregion

        #region DeleteUser
        public async Task<Result> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return Result.Failure("User not found");
                user.IsDeleted = true;
                user.UpdatedAt = DateTime.UtcNow;
                return await _context.SaveChangesAsync() > 0
                    ? Result.Success("User deleted successfully.")
                    : Result.Failure("Failed to delete user data.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application Layer Exception: {ex.Message}");
            }
        }
        #endregion

        #region Invitation
        public async Task<PagedResult<InvitationDto>> GetUserInvitationList(int id, PaginationRequest? pagination = null)
        {
            var pageNumber = pagination?.PageNumber ?? 1;
            var pageSize = pagination?.PageSize ?? 10;
            if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<InvitationDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            try
            {
                var query = _context.Invitations
                    .Include(i => i.SpaceRole)
                    .Include(i => i.MonasterySpace)
                    .Include(i => i.InvitedBy)
                    .Include(i => i.InvitedUser)
                    .AsNoTracking()
                    .Where(i => i.InvitedUserId == id);
                var totalCount = await query.CountAsync();
                var items = await query.OrderBy(x => x.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var data = items.Select(MapInvitation).ToList();
                return PagedResult<InvitationDto>.Success(data, new Pagination(pageNumber, pageSize, totalCount), "Successfully Retrieved Invitation Lists");
            }
            catch (Exception ex)
            {
                return PagedResult<InvitationDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }

        public async Task<PagedResult<InvitationDto>> GetInvitedByOtherList(int id, PaginationRequest? pagination = null)
        {
            var pageNumber = pagination?.PageNumber ?? 1;
            var pageSize = pagination?.PageSize ?? 10;
            if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<InvitationDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            try
            {
                var query = _context.Invitations
                    .Include(i => i.SpaceRole)
                    .Include(i => i.MonasterySpace)
                    .Include(i => i.InvitedBy)
                    .Include(i => i.InvitedUser)
                    .AsNoTracking()
                    .Where(i => i.InvitedById == id);
                var totalCount = await query.CountAsync();
                var items = await query.OrderBy(x => x.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var data = items.Select(MapInvitation).ToList();
                return PagedResult<InvitationDto>.Success(data, new Pagination(pageNumber, pageSize, totalCount), "Successfully Retrieved Invited Lists");
            }
            catch (Exception ex)
            {
                return PagedResult<InvitationDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }
        #endregion

        #region Notification
        public async Task<PagedResult<NotificationDto>> GetUserNotificationList(int id, PaginationRequest? pagination = null)
        {
            var pageNumber = pagination?.PageNumber ?? 1;
            var pageSize = pagination?.PageSize ?? 10;
            if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<NotificationDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            try
            {
                var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == id && n.IsDeleted == false);
                var totalCount = await query.CountAsync();
                var items = await query.OrderBy(x => x.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var data = items.Select(MapNotification).ToList();
                return PagedResult<NotificationDto>.Success(data, new Pagination(pageNumber, pageSize, totalCount), "Successfully Retrieved User's Notification Lists");
            }
            catch (Exception ex)
            {
                return PagedResult<NotificationDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }

        public async Task<Result> DeleteUserNotificationList(int id)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == id && n.IsDeleted == false).ToListAsync();
                foreach (var notification in notifications) notification.IsDelete = true;
                await _context.SaveChangesAsync();
                return Result.Success("User notifications deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application Layer Exception: {ex.Message}");
            }
        }
        #endregion

        #region MonkProfile
        public async Task<Result<MonkProfileDto>> GetMonkProfile(int currentUserId, int targetUserId)
        {
            try
            {
                var targetUser = await _context.Users.Include(u => u.Role).AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == targetUserId && u.IsDeleted == false);
                if (targetUser == null)
                {
                    return Result<MonkProfileDto>.Failure("User not found.");
                }

                if (targetUser.Role == null || !string.Equals(targetUser.Role.Name, "Monk", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<MonkProfileDto>.Failure("User is not a monk.");
                }

                var profile = await _context.MonkProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == targetUserId);
                var dto = new MonkProfileDto
                {
                    Id = profile?.Id ?? 0,
                    UserId = targetUserId,
                    MonasteryName = profile?.MonasteryName,
                    MonasteryAddress = profile?.MonasteryAddress
                };

                return Result<MonkProfileDto>.Success(dto, "Monk profile retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<MonkProfileDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }

        public async Task<Result<MonkProfileDto>> UpdateMonkProfile(int currentUserId, int targetUserId, UpdateMonkProfileRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return Result<MonkProfileDto>.Failure("Request data is required.");
                }

                var targetUser = await _context.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == targetUserId && u.IsDeleted == false);
                if (targetUser == null)
                {
                    return Result<MonkProfileDto>.Failure("User not found.");
                }

                if (targetUser.Role == null || !string.Equals(targetUser.Role.Name, "Monk", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<MonkProfileDto>.Failure("User is not a monk.");
                }

                var profile = await _context.MonkProfiles.FirstOrDefaultAsync(p => p.UserId == targetUserId);
                if (profile == null)
                {
                    profile = new MonkProfile
                    {
                        UserId = targetUserId,
                        MonasteryName = request.MonasteryName,
                        MonasteryAddress = request.MonasteryAddress
                    };
                    await _context.MonkProfiles.AddAsync(profile);
                }
                else
                {
                    if (request.MonasteryName != null)
                    {
                        profile.MonasteryName = request.MonasteryName;
                    }
                    if (request.MonasteryAddress != null)
                    {
                        profile.MonasteryAddress = request.MonasteryAddress;
                    }
                    _context.MonkProfiles.Update(profile);
                }

                await _context.SaveChangesAsync();

                var dto = new MonkProfileDto
                {
                    Id = profile.Id,
                    UserId = targetUserId,
                    MonasteryName = profile.MonasteryName,
                    MonasteryAddress = profile.MonasteryAddress
                };

                return Result<MonkProfileDto>.Success(dto, "Monk profile updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<MonkProfileDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }

        public async Task<Result> DeleteMonkProfile(int currentUserId, int targetUserId)
        {
            try
            {
                var targetUser = await _context.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == targetUserId && u.IsDeleted == false);
                if (targetUser == null)
                {
                    return Result.Failure("User not found.");
                }

                if (targetUser.Role == null || !string.Equals(targetUser.Role.Name, "Monk", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure("User is not a monk.");
                }

                var profile = await _context.MonkProfiles.FirstOrDefaultAsync(p => p.UserId == targetUserId);
                if (profile == null)
                {
                    return Result.Failure("Monk profile not found.");
                }

                _context.MonkProfiles.Remove(profile);
                await _context.SaveChangesAsync();

                return Result.Success("Monk profile deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application Layer Exception: {ex.Message}");
            }
        }

        #endregion

        #region UserSettings
        public async Task<Result<UserSettingDto>> GetUserSettings(int currentUserId, int targetUserId)
        {
            try
            {
                var targetUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetUserId && u.IsDeleted == false);
                if (targetUser == null)
                {
                    return Result<UserSettingDto>.Failure("User not found.");
                }

                var setting = await _context.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == targetUserId);

                var today = DateTime.UtcNow.Date;
                var defaultPickupNoti = today.AddHours(15).AddMinutes(30);
                var defaultDropoffNoti = today.AddHours(22);

                var dto = new UserSettingDto
                {
                    UserId = targetUserId,
                    PickupNotificationTime = setting?.PickupNotificationTime ?? defaultPickupNoti,
                    DropoffNotificationTime = setting?.DropoffNotificationTime ?? defaultDropoffNoti,
                    PickupTime = setting?.PickupTime,
                    DropoffTime = setting?.DropoffTime
                };

                return Result<UserSettingDto>.Success(dto, "User settings retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<UserSettingDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }

        public async Task<Result<UserSettingDto>> UpdateUserSettings(int currentUserId, int targetUserId, UpdateUserSettingRequestDto request)
        {
            try
            {
                var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId && u.IsDeleted == false);
                if (targetUser == null)
                {
                    return Result<UserSettingDto>.Failure("User not found.");
                }

                var setting = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == targetUserId);
                if (setting == null)
                {
                    setting = new UserSetting
                    {
                        UserId = targetUserId,
                        PickupNotificationTime = request.PickupNotificationTime,
                        DropoffNotificationTime = request.DropoffNotificationTime,
                        PickupTime = request.PickupTime,
                        DropoffTime = request.DropoffTime
                    };
                    await _context.UserSettings.AddAsync(setting);
                }
                else
                {
                    setting.PickupNotificationTime = request.PickupNotificationTime;
                    setting.DropoffNotificationTime = request.DropoffNotificationTime;
                    setting.PickupTime = request.PickupTime;
                    setting.DropoffTime = request.DropoffTime;
                    _context.UserSettings.Update(setting);
                }

                await _context.SaveChangesAsync();

                var dto = new UserSettingDto
                {
                    UserId = targetUserId,
                    PickupNotificationTime = setting.PickupNotificationTime,
                    DropoffNotificationTime = setting.DropoffNotificationTime,
                    PickupTime = setting.PickupTime,
                    DropoffTime = setting.DropoffTime
                };

                return Result<UserSettingDto>.Success(dto, "User settings updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<UserSettingDto>.Failure($"Application Layer Exception: {ex.Message}");
            }
        }
        #endregion

        private static UserEntity MapToEntity(UserDto dto)
        {
            return new UserEntity
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                RoleId = dto.RoleId,
                Email = dto.Email,
                ContactPhoneNumber = dto.ContactPhoneNumber
            };
        }

        private static UserDto MapToDto(UserEntity user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name,
                Email = user.Email,
                ContactPhoneNumber = user.ContactPhoneNumber
            };
        }

        private static InvitationDto MapInvitation(Invitation invitation)
        {
            return new InvitationDto
            {
                Id = invitation.Id,
                MonasterySpaceId = invitation.MonasterySpaceId,
                MonasterySpaceName = invitation.MonasterySpace?.MonasteryName,
                InvitedUserId = invitation.InvitedUserId,
                InvitedUserName = invitation.InvitedUser?.Name,
                InvitedById = invitation.InvitedById,
                InvitedByName = invitation.InvitedBy?.Name,
                SpaceRoleId = invitation.SpaceRoleId,
                SpaceRoleName = invitation.SpaceRole?.Name,
                Status = invitation.Status,
                CreatedAt = invitation.CreatedAt
            };
        }

        private static NotificationDto MapNotification(NotificationEntity notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.Type,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

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
    
        public Task<Result<UserDto>> GetUser(GetUserRequestDto request) => GetUser(request.Id);
        public Task<Result> DeleteUser(DeleteUserRequestDto request) => DeleteUser(request.Id);
        public Task<PagedResult<InvitationDto>> GetUserInvitationList(GetUserInvitationsRequestDto request) =>
            GetUserInvitationList(request.Id, new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize });
        public Task<PagedResult<InvitationDto>> GetInvitedByOtherList(GetInvitedByOtherRequestDto request) =>
            GetInvitedByOtherList(request.Id, new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize });
        public Task<PagedResult<NotificationDto>> GetUserNotificationList(GetUserNotificationsRequestDto request) =>
            GetUserNotificationList(request.Id, new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize });
        public Task<Result> DeleteUserNotificationList(DeleteUserNotificationsRequestDto request) =>
            DeleteUserNotificationList(request.Id);
        public Task<Result<MonkProfileDto>> GetMonkProfile(int currentUserId, GetMonkProfileRequestDto request) =>
            GetMonkProfile(currentUserId, request.UserId);
        public Task<Result<MonkProfileDto>> UpdateMonkProfile(int currentUserId, UpdateMonkProfileRequestDto request) =>
            UpdateMonkProfile(currentUserId, request?.UserId ?? 0, request!);
        public Task<Result> DeleteMonkProfile(int currentUserId, DeleteMonkProfileRequestDto request) =>
            request == null ? Task.FromResult(Result.Failure("Request data is required.")) : DeleteMonkProfile(currentUserId, request.UserId);
        public Task<Result<UserSettingDto>> GetUserSettings(int currentUserId, GetUserSettingsRequestDto request) =>
            GetUserSettings(currentUserId, request.UserId);
        public Task<Result<UserSettingDto>> UpdateUserSettings(int currentUserId, UpdateUserSettingRequestDto request) =>
            UpdateUserSettings(currentUserId, request.UserId, request);
    }
}
