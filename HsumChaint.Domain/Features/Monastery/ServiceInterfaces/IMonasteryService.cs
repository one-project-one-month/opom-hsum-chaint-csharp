using HsumChaint.Domain.Features.Monastery.DTOs;

namespace HsumChaint.Domain.Features.Monastery.ServiceInterfaces
{
    public interface IMonasteryService
    {
        Task<ApplicationCommonResponseModel<MonasterySpaceDto>> CreateMonastery(int currentUserId, CreateMonasteryRequestDto request);
        Task<ApplicationCommonResponseModel<MonasterySpaceDto>> UpdateMonastery(int currentUserId, int monasterySpaceId, UpdateMonasteryRequestDto request);
        Task<ApplicationCommonResponseModel<MonasterySpaceDto>> GetMonastery(int currentUserId, int monasterySpaceId);
        Task<ApplicationCommonResponseModel<List<MonasterySpaceDto>>> GetMyMonasteries(int currentUserId);
        Task<ApplicationCommonResponseModel<InvitationResponseDto>> InviteMember(int currentUserId, int monasterySpaceId, InviteMemberRequestDto request);
        Task<ApplicationCommonResponseModel<InvitationResponseDto>> RespondToInvitation(int currentUserId, int invitationId, RespondInvitationRequestDto request);
        Task<ApplicationCommonResponseModel<List<MonasteryMemberDto>>> GetMembers(int currentUserId, int monasterySpaceId);
        Task<ApplicationCommonResponseModel<MonasteryMemberDto>> UpdateMemberRole(int currentUserId, int monasterySpaceId, int memberUserId, UpdateMemberRoleRequestDto request);
        Task<ApplicationCommonResponseModel<MonasteryMemberDto>> RemoveMember(int currentUserId, int monasterySpaceId, int memberUserId);
    }
}
