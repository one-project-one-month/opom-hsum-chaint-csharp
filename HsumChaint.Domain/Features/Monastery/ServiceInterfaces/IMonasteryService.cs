using HsumChaint.Shared;
using HsumChaint.Domain.Features.Monastery.DTOs;
using HsumChaint.Domain.Features.RolePermission.DTOs;

namespace HsumChaint.Domain.Features.Monastery.ServiceInterfaces
{
    public interface IMonasteryService
    {
        Task<PagedResult<MonasterySpaceDto>> GetMonasteries(GetMonasteriesRequestDto request);
        Task<PagedResult<MonasterySpaceDto>> SearchMonasteries(SearchMonasteriesRequestDto request);
        Task<CursorPagedResult<MonasterySpaceDto>> GetMonasteriesFeed(CursorPaginationRequest request);
        Task<CursorPagedResult<MonasterySpaceDto>> SearchMonasteriesFeed(SearchMonasteriesFeedRequestDto request);
        Task<Result<MonasterySpaceDto>> CreateMonastery(int currentUserId, CreateMonasteryRequestDto request);
        Task<Result<MonasterySpaceDto>> UpdateMonastery(int currentUserId, UpdateMonasteryRequestDto request);
        Task<Result<bool>> DeleteMonastery(int currentUserId, DeleteMonasteryRequestDto request);
        Task<Result<MonasterySpaceDto>> GetMonastery(int currentUserId, GetMonasteryRequestDto request);
        Task<PagedResult<MonasterySpaceDto>> GetMyMonasteries(int currentUserId, PaginationRequest? pagination = null);
        Task<CursorPagedResult<MonasterySpaceDto>> GetMyMonasteriesFeed(int currentUserId, CursorPaginationRequest request);
        Task<Result<InvitationResponseDto>> InviteMember(int currentUserId, InviteMemberRequestDto request);
        Task<Result<InvitationResponseDto>> RespondToInvitation(int currentUserId, RespondInvitationRequestDto request);
        Task<PagedResult<MonasteryMemberDto>> GetMembers(int currentUserId, GetMonasteryMembersRequestDto request);
        Task<Result<MonasteryMemberDto>> UpdateMemberRole(int currentUserId, UpdateMemberRoleRequestDto request);
        Task<Result<MonasteryMemberDto>> RemoveMember(int currentUserId, RemoveMemberRequestDto request);
        Task<Result<List<SpacePermissionDto>>> GetSpacePermissions(int currentUserId, int monasterySpaceId);
        Task<Result<List<SpaceRoleDto>>> GetSpaceRoles(int currentUserId, int monasterySpaceId);
    }
}
