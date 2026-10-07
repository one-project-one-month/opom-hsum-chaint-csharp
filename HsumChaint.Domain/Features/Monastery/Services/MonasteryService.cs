using HsumChaint.Shared;
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

        public async Task<Result<MonasterySpaceDto>> CreateMonastery(int currentUserId, CreateMonasteryRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.MonasteryName))
            {
                return Result<MonasterySpaceDto>.Failure("Monastery name is required.");
            }

            var creator = await _dbContext.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsDeleted == false);
            if (creator?.Role == null || creator.Role.IsDeleted)
            {
                return Result<MonasterySpaceDto>.Failure("User or active role not found.");
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

            return Result<MonasterySpaceDto>.Success(MapMonastery(monastery, creator.RoleId, creator.Role.Name, true), "Monastery created successfully.");
        }

        public async Task<Result<MonasterySpaceDto>> UpdateMonastery(int currentUserId, int monasterySpaceId, UpdateMonasteryRequestDto request)
        {
            var member = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(member))
            {
                return Result<MonasterySpaceDto>.Failure("User is not authorized to manage this monastery.");
            }

            var monastery = await _dbContext.MonasterySpaces.FindAsync(monasterySpaceId);
            if (monastery == null)
            {
                return Result<MonasterySpaceDto>.Failure("Monastery not found.");
            }

            monastery.MonasteryName = string.IsNullOrWhiteSpace(request.MonasteryName) ? monastery.MonasteryName : request.MonasteryName;
            monastery.Description = request.Description;
            monastery.Address = request.Address;
            await _dbContext.SaveChangesAsync();

            return Result<MonasterySpaceDto>.Success(MapMonastery(monastery, member!.RoleId, member.Role?.Name, member.IsOwner == true), "Monastery updated successfully.");
        }

        public async Task<Result<MonasterySpaceDto>> GetMonastery(int currentUserId, int monasterySpaceId)
        {
            var member = await GetMember(currentUserId, monasterySpaceId);

            if (member == null)
            {
                return Result<MonasterySpaceDto>.Failure("User is not a member of this monastery.");
            }

            var monastery = await _dbContext.MonasterySpaces
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == monasterySpaceId);

            if (monastery == null)
            {
                return Result<MonasterySpaceDto>.Failure("Monastery not found.");
            }

            return Result<MonasterySpaceDto>.Success(MapMonastery(monastery, member.RoleId, member.Role?.Name, member.IsOwner == true), "Monastery retrieved successfully.");
        }

        public async Task<PagedResult<MonasterySpaceDto>> GetMyMonasteries(int currentUserId, PaginationRequest? pagination = null)
        {
            var pageNumber = pagination?.PageNumber ?? 1;
            var pageSize = pagination?.PageSize ?? 10;
            if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<MonasterySpaceDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            var query = (
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
                });

            var totalCount = await query.CountAsync();
            var monasteries = await query.OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return PagedResult<MonasterySpaceDto>.Success(monasteries, new Pagination(pageNumber, pageSize, totalCount), "Monasteries retrieved successfully.");
        }

        public async Task<Result<InvitationResponseDto>> InviteMember(int currentUserId, int monasterySpaceId, InviteMemberRequestDto request)
        {
            var inviter = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(inviter))
            {
                return Result<InvitationResponseDto>.Failure("User is not authorized to invite monastery members.");
            }

            if (!await _dbContext.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted))
            {
                return Result<InvitationResponseDto>.Failure("Role not found.");
            }

            var invitedUser = await ResolveInvitedUser(request);
            if (invitedUser == null)
            {
                return Result<InvitationResponseDto>.Failure("Invited user not found.");
            }

            if (invitedUser.Id == currentUserId)
            {
                return Result<InvitationResponseDto>.Failure("User cannot invite themselves.");
            }

            var existingMember = await GetMember(invitedUser.Id, monasterySpaceId);
            if (existingMember != null)
            {
                return Result<InvitationResponseDto>.Failure("User is already a monastery member.");
            }

            var existingInvitation = await _dbContext.Invitations
                .FirstOrDefaultAsync(x => x.MonasterySpaceId == monasterySpaceId
                    && x.InvitedUserId == invitedUser.Id
                    && x.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                return Result<InvitationResponseDto>.Failure("User already has a pending invitation.");
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

            return Result<InvitationResponseDto>.Success(MapInvitation(invitation), "Invitation created successfully.");
        }

        public async Task<Result<InvitationResponseDto>> RespondToInvitation(int currentUserId, int invitationId, RespondInvitationRequestDto request)
        {
            if (request.Status != InvitationStatus.Accept && request.Status != InvitationStatus.Reject)
            {
                return Result<InvitationResponseDto>.Failure("Invitation response must be Accept or Reject.");
            }

            var invitation = await _dbContext.Invitations.Include(i => i.Role).FirstOrDefaultAsync(i => i.Id == invitationId);
            if (invitation == null)
            {
                return Result<InvitationResponseDto>.Failure("Invitation not found.");
            }

            if (invitation.InvitedUserId != currentUserId)
            {
                return Result<InvitationResponseDto>.Failure("User is not authorized to respond to this invitation.");
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return Result<InvitationResponseDto>.Failure("Invitation has already been answered.");
            }

            invitation.Status = request.Status;

            if (request.Status == InvitationStatus.Accept)
            {
                if (invitation.Role == null || invitation.Role.IsDeleted)
                {
                    invitation.Status = InvitationStatus.Pending;
                    return Result<InvitationResponseDto>.Failure("Invitation role is inactive or not found.");
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

            return Result<InvitationResponseDto>.Success(MapInvitation(invitation), request.Status == InvitationStatus.Accept
                ? "Invitation accepted successfully."
                : "Invitation rejected successfully.");
        }

        public async Task<PagedResult<MonasteryMemberDto>> GetMembers(int currentUserId, int monasterySpaceId, PaginationRequest? pagination = null)
        {
            var pageNumber = pagination?.PageNumber ?? 1;
            var pageSize = pagination?.PageSize ?? 10;
            if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<MonasteryMemberDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            var member = await GetMember(currentUserId, monasterySpaceId);
            if (member == null) return PagedResult<MonasteryMemberDto>.Failure("User is not a member of this monastery.");
            var query = from membership in _dbContext.MonasteryMembers.AsNoTracking()
                        join user in _dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
                        where membership.MonasterySpaceId == monasterySpaceId
                        select new MonasteryMemberDto
                        {
                            Id = membership.Id,
                            UserId = user.Id,
                            UserName = user.Name,
                            PhoneNumber = user.PhoneNumber,
                            MonasterySpaceId = membership.MonasterySpaceId ?? 0,
                            RoleId = membership.RoleId,
                            RoleName = membership.Role != null ? membership.Role.Name : null,
                            IsOwner = membership.IsOwner == true
                        };
            var totalCount = await query.CountAsync();
            var members = await query.OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return PagedResult<MonasteryMemberDto>.Success(members, new Pagination(pageNumber, pageSize, totalCount), "Members retrieved successfully.");
        }

        public async Task<Result<MonasteryMemberDto>> UpdateMemberRole(int currentUserId, int monasterySpaceId, int memberUserId, UpdateMemberRoleRequestDto request)
        {
            var actor = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(actor))
            {
                return Result<MonasteryMemberDto>.Failure("User is not authorized to manage monastery members.");
            }

            if (!await _dbContext.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted))
            {
                return Result<MonasteryMemberDto>.Failure("Role not found.");
            }

            var member = await GetMember(memberUserId, monasterySpaceId);
            if (member == null)
            {
                return Result<MonasteryMemberDto>.Failure("Member not found.");
            }

            if (member.IsOwner == true)
            {
                return Result<MonasteryMemberDto>.Failure("Owner member role cannot be changed.");
            }

            member.RoleId = request.RoleId;
            await _dbContext.SaveChangesAsync();

            return Result<MonasteryMemberDto>.Success((await GetMemberDtos(monasterySpaceId)).First(x => x.UserId == memberUserId), "Member role updated successfully.");
        }

        public async Task<Result<MonasteryMemberDto>> RemoveMember(int currentUserId, int monasterySpaceId, int memberUserId)
        {
            var actor = await GetMember(currentUserId, monasterySpaceId);

            if (!await CanManageMonastery(actor))
            {
                return Result<MonasteryMemberDto>.Failure("User is not authorized to remove monastery members.");
            }

            var member = await GetMember(memberUserId, monasterySpaceId);
            if (member == null)
            {
                return Result<MonasteryMemberDto>.Failure("Member not found.");
            }

            if (member.IsOwner == true)
            {
                return Result<MonasteryMemberDto>.Failure("Owner member cannot be removed.");
            }

            var dto = (await GetMemberDtos(monasterySpaceId)).First(x => x.UserId == memberUserId);
            _dbContext.MonasteryMembers.Remove(member);
            await _dbContext.SaveChangesAsync();

            return Result<MonasteryMemberDto>.Success(dto, "Member removed successfully.");
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
