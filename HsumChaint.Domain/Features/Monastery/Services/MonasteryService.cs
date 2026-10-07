using HsumChaint.Shared.Authorization;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Monastery.DTOs;
using HsumChaint.Domain.Features.Monastery.ServiceInterfaces;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = HsumChaint.Database.Models.Notification;
using UserEntity = HsumChaint.Database.Models.User;

namespace HsumChaint.Domain.Features.Monastery.Services
{
    public class MonasteryService : IMonasteryService
    {
        private readonly AppDbContext _dbContext;

        public MonasteryService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApplicationCommonResponseModel<MonasterySpaceDto>> CreateMonastery(int currentUserId, CreateMonasteryRequestDto request)
        {
            var response = new ApplicationCommonResponseModel<MonasterySpaceDto>();

            if (string.IsNullOrWhiteSpace(request.MonasteryName))
            {
                response.IsSuccess = false;
                response.Message = "Monastery name is required.";
                return response;
            }

            var creator = await _dbContext.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsDeleted == false);
            if (creator?.Role == null || creator.Role.IsDeleted)
            {
                response.IsSuccess = false;
                response.Message = "User or active role not found.";
                return response;
            }
            var monastery = new MonasterySpace
            {
                MonasteryName = request.MonasteryName,
                Description = request.Description,
                Address = request.Address,
                CreatedById = currentUserId
            };

            await _dbContext.MonasterySpaces.AddAsync(monastery);
            await _dbContext.SaveChangesAsync();

            await _dbContext.MonasteryMembers.AddAsync(new MonasteryMember
            {
                UserId = currentUserId,
                MonasterySpaceId = monastery.Id,
                RoleId = creator.RoleId,
                IsOwner = true
            });
            await _dbContext.SaveChangesAsync();

            response.IsSuccess = true;
            response.Message = "Monastery created successfully.";
            response.Data = MapMonastery(monastery, creator.RoleId, creator.Role.Name, true);
            return response;
        }

        public async Task<ApplicationCommonResponseModel<MonasterySpaceDto>> UpdateMonastery(int currentUserId, int monasterySpaceId, UpdateMonasteryRequestDto request)
        {
            var response = new ApplicationCommonResponseModel<MonasterySpaceDto>();
            var member = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(member))
            {
                response.IsSuccess = false;
                response.Message = "User is not authorized to manage this monastery.";
                return response;
            }

            var monastery = await _dbContext.MonasterySpaces.FindAsync(monasterySpaceId);
            if (monastery == null)
            {
                response.IsSuccess = false;
                response.Message = "Monastery not found.";
                return response;
            }

            monastery.MonasteryName = string.IsNullOrWhiteSpace(request.MonasteryName) ? monastery.MonasteryName : request.MonasteryName;
            monastery.Description = request.Description;
            monastery.Address = request.Address;
            await _dbContext.SaveChangesAsync();

            response.IsSuccess = true;
            response.Message = "Monastery updated successfully.";
            response.Data = MapMonastery(monastery, member!.RoleId, member.Role?.Name, member.IsOwner == true);
            return response;
        }

        public async Task<ApplicationCommonResponseModel<MonasterySpaceDto>> GetMonastery(int currentUserId, int monasterySpaceId)
        {
            var response = new ApplicationCommonResponseModel<MonasterySpaceDto>();
            var member = await GetMember(currentUserId, monasterySpaceId);

            if (member == null)
            {
                response.IsSuccess = false;
                response.Message = "User is not a member of this monastery.";
                return response;
            }

            var monastery = await _dbContext.MonasterySpaces
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == monasterySpaceId);

            if (monastery == null)
            {
                response.IsSuccess = false;
                response.Message = "Monastery not found.";
                return response;
            }

            response.IsSuccess = true;
            response.Message = "Monastery retrieved successfully.";
            response.Data = MapMonastery(monastery, member.RoleId, member.Role?.Name, member.IsOwner == true);
            return response;
        }

        public async Task<ApplicationCommonResponseModel<List<MonasterySpaceDto>>> GetMyMonasteries(int currentUserId)
        {
            var response = new ApplicationCommonResponseModel<List<MonasterySpaceDto>>();

            var monasteries = await (
                from member in _dbContext.MonasteryMembers.AsNoTracking()
                join monastery in _dbContext.MonasterySpaces.AsNoTracking()
                    on member.MonasterySpaceId equals monastery.Id
                where member.UserId == currentUserId
                select new MonasterySpaceDto
                {
                    Id = monastery.Id,
                    MonasteryName = monastery.MonasteryName,
                    Description = monastery.Description,
                    Address = monastery.Address,
                    CreatedById = monastery.CreatedById,
                    CurrentUserRoleId = member.RoleId,
                    CurrentUserRoleName = member.Role != null ? member.Role.Name : null,
                    IsOwner = member.IsOwner == true
                }).ToListAsync();

            response.IsSuccess = true;
            response.Message = monasteries.Count > 0 ? "Monasteries retrieved successfully." : "Monastery list not found.";
            response.ListData = monasteries;
            return response;
        }

        public async Task<ApplicationCommonResponseModel<InvitationResponseDto>> InviteMember(int currentUserId, int monasterySpaceId, InviteMemberRequestDto request)
        {
            var response = new ApplicationCommonResponseModel<InvitationResponseDto>();
            var inviter = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(inviter))
            {
                response.IsSuccess = false;
                response.Message = "User is not authorized to invite monastery members.";
                return response;
            }

            if (!await _dbContext.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted))
            {
                response.IsSuccess = false;
                response.Message = "Role not found.";
                return response;
            }

            var invitedUser = await ResolveInvitedUser(request);
            if (invitedUser == null)
            {
                response.IsSuccess = false;
                response.Message = "Invited user not found.";
                return response;
            }

            if (invitedUser.Id == currentUserId)
            {
                response.IsSuccess = false;
                response.Message = "User cannot invite themselves.";
                return response;
            }

            var existingMember = await GetMember(invitedUser.Id, monasterySpaceId);
            if (existingMember != null)
            {
                response.IsSuccess = false;
                response.Message = "User is already a monastery member.";
                return response;
            }

            var existingInvitation = await _dbContext.Invitations
                .FirstOrDefaultAsync(x => x.MonasterySpaceId == monasterySpaceId
                    && x.InvitedUserId == invitedUser.Id
                    && x.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                response.IsSuccess = false;
                response.Message = "User already has a pending invitation.";
                return response;
            }

            var invitation = new Invitation
            {
                MonasterySpaceId = monasterySpaceId,
                InvitedUserId = invitedUser.Id,
                InvitedById = currentUserId,
                RoleId = request.RoleId,
                Role = await _dbContext.Roles.FindAsync(request.RoleId),
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Invitations.AddAsync(invitation);
            await AddNotification(invitedUser.Id, NotificationType.Invitation, "You have a new monastery invitation.");
            await _dbContext.SaveChangesAsync();

            response.IsSuccess = true;
            response.Message = "Invitation created successfully.";
            response.Data = MapInvitation(invitation);
            return response;
        }

        public async Task<ApplicationCommonResponseModel<InvitationResponseDto>> RespondToInvitation(int currentUserId, int invitationId, RespondInvitationRequestDto request)
        {
            var response = new ApplicationCommonResponseModel<InvitationResponseDto>();

            if (request.Status != InvitationStatus.Accept && request.Status != InvitationStatus.Reject)
            {
                response.IsSuccess = false;
                response.Message = "Invitation response must be Accept or Reject.";
                return response;
            }

            var invitation = await _dbContext.Invitations.Include(i => i.Role).FirstOrDefaultAsync(i => i.Id == invitationId);
            if (invitation == null)
            {
                response.IsSuccess = false;
                response.Message = "Invitation not found.";
                return response;
            }

            if (invitation.InvitedUserId != currentUserId)
            {
                response.IsSuccess = false;
                response.Message = "User is not authorized to respond to this invitation.";
                return response;
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                response.IsSuccess = false;
                response.Message = "Invitation has already been answered.";
                return response;
            }

            invitation.Status = request.Status;

            if (request.Status == InvitationStatus.Accept)
            {
                if (invitation.Role == null || invitation.Role.IsDeleted)
                {
                    invitation.Status = InvitationStatus.Pending;
                    response.IsSuccess = false;
                    response.Message = "Invitation role is inactive or not found.";
                    return response;
                }
                var existingMember = await GetMember(currentUserId, invitation.MonasterySpaceId ?? 0);
                if (existingMember == null)
                {
                    await _dbContext.MonasteryMembers.AddAsync(new MonasteryMember
                    {
                        UserId = currentUserId,
                        MonasterySpaceId = invitation.MonasterySpaceId,
                        RoleId = invitation.RoleId,
                        IsOwner = false
                    });
                }
            }

            await _dbContext.SaveChangesAsync();

            response.IsSuccess = true;
            response.Message = request.Status == InvitationStatus.Accept
                ? "Invitation accepted successfully."
                : "Invitation rejected successfully.";
            response.Data = MapInvitation(invitation);
            return response;
        }

        public async Task<ApplicationCommonResponseModel<List<MonasteryMemberDto>>> GetMembers(int currentUserId, int monasterySpaceId)
        {
            var response = new ApplicationCommonResponseModel<List<MonasteryMemberDto>>();
            var member = await GetMember(currentUserId, monasterySpaceId);

            if (member == null)
            {
                response.IsSuccess = false;
                response.Message = "User is not a member of this monastery.";
                return response;
            }

            response.IsSuccess = true;
            response.Message = "Members retrieved successfully.";
            response.ListData = await GetMemberDtos(monasterySpaceId);
            return response;
        }

        public async Task<ApplicationCommonResponseModel<MonasteryMemberDto>> UpdateMemberRole(int currentUserId, int monasterySpaceId, int memberUserId, UpdateMemberRoleRequestDto request)
        {
            var response = new ApplicationCommonResponseModel<MonasteryMemberDto>();
            var actor = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(actor))
            {
                response.IsSuccess = false;
                response.Message = "User is not authorized to manage monastery members.";
                return response;
            }

            if (!await _dbContext.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted))
            {
                response.IsSuccess = false;
                response.Message = "Role not found.";
                return response;
            }

            var member = await GetMember(memberUserId, monasterySpaceId);
            if (member == null)
            {
                response.IsSuccess = false;
                response.Message = "Member not found.";
                return response;
            }

            if (member.IsOwner == true)
            {
                response.IsSuccess = false;
                response.Message = "Owner member role cannot be changed.";
                return response;
            }

            member.RoleId = request.RoleId;
            await _dbContext.SaveChangesAsync();

            response.IsSuccess = true;
            response.Message = "Member role updated successfully.";
            response.Data = (await GetMemberDtos(monasterySpaceId)).First(x => x.UserId == memberUserId);
            return response;
        }

        public async Task<ApplicationCommonResponseModel<MonasteryMemberDto>> RemoveMember(int currentUserId, int monasterySpaceId, int memberUserId)
        {
            var response = new ApplicationCommonResponseModel<MonasteryMemberDto>();
            var actor = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(actor))
            {
                response.IsSuccess = false;
                response.Message = "User is not authorized to remove monastery members.";
                return response;
            }

            var member = await GetMember(memberUserId, monasterySpaceId);
            if (member == null)
            {
                response.IsSuccess = false;
                response.Message = "Member not found.";
                return response;
            }

            if (member.IsOwner == true)
            {
                response.IsSuccess = false;
                response.Message = "Owner member cannot be removed.";
                return response;
            }

            var dto = (await GetMemberDtos(monasterySpaceId)).First(x => x.UserId == memberUserId);
            _dbContext.MonasteryMembers.Remove(member);
            await _dbContext.SaveChangesAsync();

            response.IsSuccess = true;
            response.Message = "Member removed successfully.";
            response.Data = dto;
            return response;
        }

        private async Task<MonasteryMember?> GetMember(int userId, int monasterySpaceId)
        {
            return await _dbContext.MonasteryMembers.Include(m => m.Role)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.MonasterySpaceId == monasterySpaceId);
        }

        private async Task<List<MonasteryMemberDto>> GetMemberDtos(int monasterySpaceId)
        {
            return await (
                from member in _dbContext.MonasteryMembers.AsNoTracking()
                join user in _dbContext.Users.AsNoTracking()
                    on member.UserId equals user.Id
                where member.MonasterySpaceId == monasterySpaceId
                select new MonasteryMemberDto
                {
                    Id = member.Id,
                    UserId = user.Id,
                    UserName = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    MonasterySpaceId = member.MonasterySpaceId ?? 0,
                    RoleId = member.RoleId,
                    RoleName = member.Role != null ? member.Role.Name : null,
                    IsOwner = member.IsOwner == true
                }).ToListAsync();
        }

        private async Task<UserEntity?> ResolveInvitedUser(InviteMemberRequestDto request)
        {
            if (request.UserId.HasValue)
            {
                return await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.UserId.Value && x.IsDeleted == false);
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return await _dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber && x.IsDeleted == false);
            }

            return null;
        }

        private async Task AddNotification(int userId, NotificationType type, string message)
        {
            await _dbContext.Notifications.AddAsync(new NotificationEntity
            {
                UserId = userId,
                Type = type,
                Message = message,
                IsRead = false,
                IsDelete = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task<bool> CanManageMonastery(MonasteryMember? member)
        {
            return member is not null && (member.IsOwner == true || await _dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == member.RoleId && !rp.IsDeleted && !rp.Role.IsDeleted && !rp.Permission.IsDeleted && rp.Permission.Name == Permissions.Monastery.ManageMembers));
        }

        private static MonasterySpaceDto MapMonastery(MonasterySpace monastery, int? roleId, string? roleName, bool isOwner)
        {
            return new MonasterySpaceDto
            {
                Id = monastery.Id,
                MonasteryName = monastery.MonasteryName,
                Description = monastery.Description,
                Address = monastery.Address,
                CreatedById = monastery.CreatedById,
                CurrentUserRoleId = roleId,
                CurrentUserRoleName = roleName,
                IsOwner = isOwner
            };
        }

        private static InvitationResponseDto MapInvitation(Invitation invitation)
        {
            return new InvitationResponseDto
            {
                InvitationId = invitation.Id,
                MonasterySpaceId = invitation.MonasterySpaceId ?? 0,
                InvitedUserId = invitation.InvitedUserId ?? 0,
                InvitedById = invitation.InvitedById ?? 0,
                RoleId = invitation.RoleId,
                RoleName = invitation.Role?.Name,
                Status = invitation.Status,
                CreatedAt = invitation.CreatedAt
            };
        }
    }
}
