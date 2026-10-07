using HsumChaint.Shared;
using HsumChaint.Domain.Features.Monastery.DTOs;

namespace HsumChaint.Domain.Features.Monastery.ServiceInterfaces
{
    public interface IMonasteryService
    {
        Task<Result<MonasterySpaceDto>> CreateMonastery(int currentUserId, CreateMonasteryRequestDto request);
        Task<Result<MonasterySpaceDto>> UpdateMonastery(int currentUserId, int monasterySpaceId, UpdateMonasteryRequestDto request);
        Task<Result<MonasterySpaceDto>> GetMonastery(int currentUserId, int monasterySpaceId);
        Task<PagedResult<MonasterySpaceDto>> GetMyMonasteries(int currentUserId, PaginationRequest? pagination = null);
        Task<Result<InvitationResponseDto>> InviteMember(int currentUserId, int monasterySpaceId, InviteMemberRequestDto request);
        Task<Result<InvitationResponseDto>> RespondToInvitation(int currentUserId, int invitationId, RespondInvitationRequestDto request);
        Task<PagedResult<MonasteryMemberDto>> GetMembers(int currentUserId, int monasterySpaceId, PaginationRequest? pagination = null);
        Task<Result<MonasteryMemberDto>> UpdateMemberRole(int currentUserId, int monasterySpaceId, int memberUserId, UpdateMemberRoleRequestDto request);
        Task<Result<MonasteryMemberDto>> RemoveMember(int currentUserId, int monasterySpaceId, int memberUserId);
    }
}
