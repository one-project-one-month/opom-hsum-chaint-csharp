using HsumChaint.Shared;
using HsumChaint.Shared.Authorization;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Monastery.DTOs;
using HsumChaint.Domain.Features.Monastery.ServiceInterfaces;
using HsumChaint.Domain.Features.RolePermission.DTOs;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = HsumChaint.Database.Models.Notification;
using UserEntity = HsumChaint.Database.Models.User;

namespace HsumChaint.Domain.Features.Monastery.Services
{
    public class MonasteryService(AppDbContext dbContext) : IMonasteryService
    {
        private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        private IQueryable<MonasterySpace> ActiveMonasteries => _dbContext.MonasterySpaces.Where(x => !x.IsDeleted);

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

            if (!string.Equals(creator.Role.Name, "Monk", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(creator.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Result<MonasterySpaceDto>.Failure("Only monks can create a monastery space.");
            }

            var monastery = new MonasterySpace
            {
                MonasteryName = request.MonasteryName,
                Description = request.Description,
                Address = request.Address,
                CreatedById = currentUserId
            };

            var member = new MonasteryMember
            {
                UserId = currentUserId,
                MonasterySpace = monastery,
                SpaceRoleId = SpaceRoles.OwnerId,
                IsOwner = true
            };

            await _dbContext.MonasterySpaces.AddAsync(monastery);
            await _dbContext.MonasteryMembers.AddAsync(member);
            await _dbContext.SaveChangesAsync();

            return Result<MonasterySpaceDto>.Success(MapMonastery(monastery, SpaceRoles.OwnerId, SpaceRoles.Owner, true), "Monastery created successfully.");
        }

        public async Task<Result<MonasterySpaceDto>> UpdateMonastery(int currentUserId, UpdateMonasteryRequestDto request)
        {
            var monasterySpaceId = request.Id;
            var member = await GetMember(currentUserId, monasterySpaceId);

            if (!await HasPermissionTo(currentUserId, member, SpacePermissions.SpaceUpdate))
            {
                return Result<MonasterySpaceDto>.Failure("User is not authorized to manage this monastery.");
            }

            var monastery = await ActiveMonasteries.FirstOrDefaultAsync(x => x.Id == monasterySpaceId);
            if (monastery == null)
            {
                return Result<MonasterySpaceDto>.Failure("Monastery not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.MonasteryName))
            {
                monastery.MonasteryName = request.MonasteryName;
            }
            if (request.Description != null)
            {
                monastery.Description = request.Description;
            }
            if (request.Address != null)
            {
                monastery.Address = request.Address;
            }
            await _dbContext.SaveChangesAsync();

            return Result<MonasterySpaceDto>.Success(MapMonastery(monastery, member!.SpaceRoleId, member.SpaceRole?.Name, member.IsOwner == true), "Monastery updated successfully.");
        }

        public Task<Result<MonasterySpaceDto>> UpdateMonastery(int currentUserId, int monasterySpaceId, UpdateMonasteryRequestDto request)
        {
            request.Id = monasterySpaceId;
            return UpdateMonastery(currentUserId, request);
        }

        public async Task<Result<bool>> DeleteMonastery(int currentUserId, DeleteMonasteryRequestDto request)
        {
            var monasterySpaceId = request.Id;
            var monastery = await ActiveMonasteries.FirstOrDefaultAsync(x => x.Id == monasterySpaceId);
            if (monastery == null)
            {
                return Result<bool>.Failure("Monastery not found.");
            }

            var member = await GetMember(currentUserId, monasterySpaceId);
            if (!await CanDeleteMonastery(member))
            {
                return Result<bool>.Failure("User is not authorized to delete this monastery. Only the monastery owner (Monk) can delete the space.");
            }

            monastery.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            return Result<bool>.Success(true, "Monastery deleted successfully.");
        }

        public Task<Result<bool>> DeleteMonastery(int currentUserId, int monasterySpaceId)
        {
            return DeleteMonastery(currentUserId, new DeleteMonasteryRequestDto { Id = monasterySpaceId });
        }

        public async Task<Result<MonasterySpaceDto>> GetMonastery(int currentUserId, GetMonasteryRequestDto request)
        {
            var monasterySpaceId = request.Id;
            var member = await GetMember(currentUserId, monasterySpaceId);
            if (member == null)
            {
                return Result<MonasterySpaceDto>.Failure("Monastery not found.");
            }

            var monastery = await ActiveMonasteries.FirstOrDefaultAsync(x => x.Id == monasterySpaceId);
            if (monastery == null)
            {
                return Result<MonasterySpaceDto>.Failure("Monastery not found.");
            }

            return Result<MonasterySpaceDto>.Success(MapMonastery(monastery, member.SpaceRoleId, member.SpaceRole?.Name, member.IsOwner == true), "Monastery retrieved successfully.");
        }

        public Task<Result<MonasterySpaceDto>> GetMonastery(int currentUserId, int monasterySpaceId)
        {
            return GetMonastery(currentUserId, new GetMonasteryRequestDto { Id = monasterySpaceId });
        }

        public async Task<PagedResult<MonasterySpaceDto>> GetMonasteries(GetMonasteriesRequestDto request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            if (((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<MonasterySpaceDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            var query = ActiveMonasteries.AsNoTracking();
            var totalCount = await query.CountAsync();
            var monasteries = await query.OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new MonasterySpaceDto
                {
                    Id = x.Id,
                    MonasteryName = x.MonasteryName,
                    Description = x.Description,
                    Address = x.Address,
                    CreatedById = x.CreatedById
                })
                .ToListAsync();

            return PagedResult<MonasterySpaceDto>.Success(monasteries, new Pagination(pageNumber, pageSize, totalCount), "Monasteries retrieved successfully.");
        }

        public async Task<PagedResult<MonasterySpaceDto>> SearchMonasteries(SearchMonasteriesRequestDto request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            if (((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<MonasterySpaceDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            var query = ActiveMonasteries.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var term = request.Keyword.Trim();
                query = query.Where(x => (x.MonasteryName != null && x.MonasteryName.Contains(term)) ||
                                         (x.Address != null && x.Address.Contains(term)));
            }

            var totalCount = await query.CountAsync();
            var monasteries = await query.OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new MonasterySpaceDto
                {
                    Id = x.Id,
                    MonasteryName = x.MonasteryName,
                    Description = x.Description,
                    Address = x.Address,
                    CreatedById = x.CreatedById
                })
                .ToListAsync();

            return PagedResult<MonasterySpaceDto>.Success(monasteries, new Pagination(pageNumber, pageSize, totalCount), "Monasteries retrieved successfully.");
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
                join monastery in ActiveMonasteries.AsNoTracking()
                    on member.MonasterySpaceId equals monastery.Id
                where member.UserId == currentUserId
                select new MonasterySpaceDto
                {
                    Id = monastery.Id,
                    MonasteryName = monastery.MonasteryName,
                    Description = monastery.Description,
                    Address = monastery.Address,
                    CreatedById = monastery.CreatedById,
                    CurrentUserRoleId = member.SpaceRoleId,
                    CurrentUserRoleName = member.SpaceRole != null ? member.SpaceRole.Name : null,
                    IsOwner = member.IsOwner == true
                });

            var totalCount = await query.CountAsync();
            var monasteries = await query.OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return PagedResult<MonasterySpaceDto>.Success(monasteries, new Pagination(pageNumber, pageSize, totalCount), "Monasteries retrieved successfully.");
        }

        public async Task<Result<InvitationResponseDto>> InviteMember(int currentUserId, InviteMemberRequestDto request)
        {
            var monasterySpaceId = request.MonasterySpaceId;
            var inviter = await GetMember(currentUserId, monasterySpaceId);

            if (!await HasPermissionTo(currentUserId, inviter, SpacePermissions.SpaceInviteMember))
            {
                return Result<InvitationResponseDto>.Failure("User is not authorized to invite monastery members.");
            }

            if (request.SpaceRoleId == SpaceRoles.OwnerId)
            {
                return Result<InvitationResponseDto>.Failure("Cannot invite a member as Owner.");
            }

            var spaceRole = await _dbContext.SpaceRoles.FirstOrDefaultAsync(r => r.Id == request.SpaceRoleId && !r.IsDeleted);
            if (spaceRole == null)
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
                SpaceRoleId = request.SpaceRoleId,
                SpaceRole = spaceRole,
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Invitations.AddAsync(invitation);
            await AddNotification(invitedUser.Id, NotificationType.Invitation, "You have a new monastery invitation.");
            await _dbContext.SaveChangesAsync();

            return Result<InvitationResponseDto>.Success(MapInvitation(invitation), "Invitation created successfully.");
        }

        public async Task<Result<InvitationResponseDto>> RespondToInvitation(int currentUserId, RespondInvitationRequestDto request)
        {
            var invitationId = request.InvitationId;
            if (request.Status != InvitationStatus.Accept && request.Status != InvitationStatus.Reject)
            {
                return Result<InvitationResponseDto>.Failure("Invitation response must be Accept or Reject.");
            }

            var invitation = await _dbContext.Invitations.Include(i => i.SpaceRole).FirstOrDefaultAsync(i => i.Id == invitationId);
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
                if (invitation.SpaceRole == null || invitation.SpaceRole.IsDeleted)
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
                        SpaceRoleId = invitation.SpaceRoleId,
                        IsOwner = false
                    });
                }
            }

            await _dbContext.SaveChangesAsync();

            return Result<InvitationResponseDto>.Success(MapInvitation(invitation), request.Status == InvitationStatus.Accept
                ? "Invitation accepted successfully."
                : "Invitation rejected successfully.");
        }

        public async Task<PagedResult<MonasteryMemberDto>> GetMembers(int currentUserId, GetMonasteryMembersRequestDto request)
        {
            var monasterySpaceId = request.MonasterySpaceId;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            if (((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<MonasteryMemberDto>.Failure("Page number and page size must be positive and within the supported range.");
            }

            var member = await GetMember(currentUserId, monasterySpaceId);
            if (!await HasPermissionTo(currentUserId, member, SpacePermissions.SpaceViewMembers))
            {
                return PagedResult<MonasteryMemberDto>.Failure("User is not authorized to view members of this monastery.");
            }
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
                            SpaceRoleId = membership.SpaceRoleId,
                            SpaceRoleName = membership.SpaceRole != null ? membership.SpaceRole.Name : null,
                            IsOwner = membership.IsOwner == true
                        };
            var totalCount = await query.CountAsync();
            var members = await query.OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return PagedResult<MonasteryMemberDto>.Success(members, new Pagination(pageNumber, pageSize, totalCount), "Members retrieved successfully.");
        }

        public async Task<Result<MonasteryMemberDto>> UpdateMemberRole(int currentUserId, UpdateMemberRoleRequestDto request)
        {
            var monasterySpaceId = request.MonasterySpaceId;
            var memberUserId = request.MemberUserId;
            var actor = await GetMember(currentUserId, monasterySpaceId);

            if (!await HasPermissionTo(currentUserId, actor, SpacePermissions.SpaceUpdateMemberRole))
            {
                return Result<MonasteryMemberDto>.Failure("User is not authorized to manage monastery members.");
            }

            if (request.SpaceRoleId == SpaceRoles.OwnerId)
            {
                return Result<MonasteryMemberDto>.Failure("Cannot promote a member to Owner.");
            }

            if (!await _dbContext.SpaceRoles.AnyAsync(r => r.Id == request.SpaceRoleId && !r.IsDeleted))
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

            member.SpaceRoleId = request.SpaceRoleId;
            await _dbContext.SaveChangesAsync();

            return Result<MonasteryMemberDto>.Success((await GetMemberDtos(monasterySpaceId)).First(x => x.UserId == memberUserId), "Member role updated successfully.");
        }

        public async Task<Result<MonasteryMemberDto>> RemoveMember(int currentUserId, RemoveMemberRequestDto request)
        {
            var monasterySpaceId = request.MonasterySpaceId;
            var memberUserId = request.MemberUserId;
            var actor = await GetMember(currentUserId, monasterySpaceId);

            if (!await HasPermissionTo(currentUserId, actor, SpacePermissions.SpaceRemoveMember))
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

        public Task<Result<InvitationResponseDto>> InviteMember(int currentUserId, int monasterySpaceId, InviteMemberRequestDto request)
        {
            request.MonasterySpaceId = monasterySpaceId;
            return InviteMember(currentUserId, request);
        }

        public Task<Result<InvitationResponseDto>> RespondToInvitation(int currentUserId, int invitationId, RespondInvitationRequestDto request)
        {
            request.InvitationId = invitationId;
            return RespondToInvitation(currentUserId, request);
        }

        public Task<PagedResult<MonasteryMemberDto>> GetMembers(int currentUserId, int monasterySpaceId, PaginationRequest? pagination = null)
        {
            return GetMembers(currentUserId, new GetMonasteryMembersRequestDto
            {
                MonasterySpaceId = monasterySpaceId,
                PageNumber = pagination?.PageNumber ?? 1,
                PageSize = pagination?.PageSize ?? 10
            });
        }

        public Task<Result<MonasteryMemberDto>> UpdateMemberRole(int currentUserId, int monasterySpaceId, int memberUserId, UpdateMemberRoleRequestDto request)
        {
            request.MonasterySpaceId = monasterySpaceId;
            request.MemberUserId = memberUserId;
            return UpdateMemberRole(currentUserId, request);
        }

        public Task<Result<MonasteryMemberDto>> RemoveMember(int currentUserId, int monasterySpaceId, int memberUserId)
        {
            return RemoveMember(currentUserId, new RemoveMemberRequestDto
            {
                MonasterySpaceId = monasterySpaceId,
                MemberUserId = memberUserId
            });
        }

        private async Task<MonasteryMember?> GetMember(int userId, int monasterySpaceId)
        {
            return await _dbContext.MonasteryMembers.Include(m => m.SpaceRole)
                .FirstOrDefaultAsync(x => x.UserId == userId
                    && x.MonasterySpaceId == monasterySpaceId
                    && _dbContext.MonasterySpaces.Any(s => s.Id == monasterySpaceId && !s.IsDeleted));
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
                    SpaceRoleId = member.SpaceRoleId,
                    SpaceRoleName = member.SpaceRole != null ? member.SpaceRole.Name : null,
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

        private async Task<bool> HasPermissionTo(int currentUserId, MonasteryMember? member, string permissionName)
        {
            if (member is not null && (member.IsOwner == true || await _dbContext.SpaceRolePermissions.AnyAsync(srp => srp.SpaceRoleId == member.SpaceRoleId && !srp.IsDeleted && !srp.SpaceRole.IsDeleted && !srp.SpacePermission.IsDeleted && srp.SpacePermission.Name == permissionName)))
            {
                return true;
            }

            var user = await _dbContext.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsDeleted == false);
            return user?.Role != null && !user.Role.IsDeleted && string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> CanDeleteMonastery(MonasteryMember? member)
        {
            if (member == null || member.IsOwner != true) return false;
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == member.UserId && u.IsDeleted != true);
            return user != null && (user.RoleId == 1 || user.RoleId == 2);
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
                SpaceRoleId = invitation.SpaceRoleId,
                SpaceRoleName = invitation.SpaceRole?.Name,
                Status = invitation.Status,
                CreatedAt = invitation.CreatedAt
            };
        }

        public async Task<Result<List<SpacePermissionDto>>> GetSpacePermissions(int currentUserId, int monasterySpaceId)
        {
            var monastery = await ActiveMonasteries.FirstOrDefaultAsync(x => x.Id == monasterySpaceId);
            if (monastery == null)
            {
                return Result<List<SpacePermissionDto>>.Failure("Monastery not found.");
            }

            var member = await GetMember(currentUserId, monasterySpaceId);
            var user = await _dbContext.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsDeleted == false);
            var isSystemAdmin = user?.Role != null && !user.Role.IsDeleted && string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase);

            if (member == null && !isSystemAdmin)
            {
                return Result<List<SpacePermissionDto>>.Failure("User is not authorized to view monastery permissions.");
            }

            if (member?.IsOwner == true || isSystemAdmin)
            {
                var allPermissions = await _dbContext.SpacePermissions
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Id)
                    .Select(p => new SpacePermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description
                    })
                    .ToListAsync();

                return Result<List<SpacePermissionDto>>.Success(allPermissions, "Space permissions retrieved successfully.");
            }

            var memberPermissions = await (
                from srp in _dbContext.SpaceRolePermissions.AsNoTracking()
                join sp in _dbContext.SpacePermissions.AsNoTracking() on srp.SpacePermissionId equals sp.Id
                where srp.SpaceRoleId == member!.SpaceRoleId
                      && !srp.IsDeleted
                      && !srp.SpaceRole.IsDeleted
                      && !sp.IsDeleted
                orderby sp.Id
                select new SpacePermissionDto
                {
                    Id = sp.Id,
                    Name = sp.Name,
                    Description = sp.Description
                }).Distinct().ToListAsync();

            return Result<List<SpacePermissionDto>>.Success(memberPermissions, "Space permissions retrieved successfully.");
        }

        public async Task<Result<List<SpaceRoleDto>>> GetSpaceRoles(int currentUserId, int monasterySpaceId)
        {
            var monastery = await ActiveMonasteries.FirstOrDefaultAsync(x => x.Id == monasterySpaceId);
            if (monastery == null)
            {
                return Result<List<SpaceRoleDto>>.Failure("Monastery not found.");
            }

            var member = await GetMember(currentUserId, monasterySpaceId);
            var user = await _dbContext.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsDeleted == false);
            var isSystemAdmin = user?.Role != null && !user.Role.IsDeleted && string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase);

            if (member == null && !isSystemAdmin)
            {
                return Result<List<SpaceRoleDto>>.Failure("User is not authorized to view monastery roles.");
            }

            var roles = await _dbContext.SpaceRoles
                .AsNoTracking()
                .Where(r => !r.IsDeleted)
                .Include(r => r.SpaceRolePermissions)
                .ThenInclude(srp => srp.SpacePermission)
                .OrderBy(r => r.Id)
                .ToListAsync();

            var roleDtos = roles.Select(role => new SpaceRoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.SpaceRolePermissions
                    .Where(rp => !rp.IsDeleted && !rp.SpacePermission.IsDeleted)
                    .Select(rp => new SpacePermissionDto
                    {
                        Id = rp.SpacePermissionId,
                        Name = rp.SpacePermission.Name,
                        Description = rp.SpacePermission.Description
                    })
                    .OrderBy(p => p.Id)
                    .ToList()
            }).ToList();

            return Result<List<SpaceRoleDto>>.Success(roleDtos, "Space roles retrieved successfully.");
        }
    }
}
