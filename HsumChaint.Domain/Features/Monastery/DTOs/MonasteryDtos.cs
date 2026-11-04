using HsumChaint.Shared;
using HsumChaint.Shared.CommonEnum;

namespace HsumChaint.Domain.Features.Monastery.DTOs
{
    public class GetMonasteriesRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SearchMonasteriesRequestDto
    {
        public string? Keyword { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SearchMonasteriesFeedRequestDto : CursorPaginationRequest
    {
        public string? Keyword { get; set; }
    }

    public class GetMonasteryRequestDto
    {
        public int Id { get; set; }
    }

    public class CreateMonasteryRequestDto
    {
        public string? MonasteryName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
    }

    public class UpdateMonasteryRequestDto
    {
        public int Id { get; set; }
        public string? MonasteryName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
    }

    public class DeleteMonasteryRequestDto
    {
        public int Id { get; set; }
    }

    public class MonasterySpaceDto
    {
        public int Id { get; set; }
        public string? MonasteryName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public int? CreatedById { get; set; }
        public int? CurrentUserRoleId { get; set; }
        public string? CurrentUserRoleName { get; set; }
        public bool IsOwner { get; set; }
    }

    public class InviteMemberRequestDto
    {
        public int MonasterySpaceId { get; set; }
        public int? UserId { get; set; }
        public string? PhoneNumber { get; set; }
        public int SpaceRoleId { get; set; }
    }

    public class InvitationResponseDto
    {
        public int InvitationId { get; set; }
        public int MonasterySpaceId { get; set; }
        public int InvitedUserId { get; set; }
        public int InvitedById { get; set; }
        public int SpaceRoleId { get; set; }
        public string? SpaceRoleName { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class RespondInvitationRequestDto
    {
        public int InvitationId { get; set; }
        public InvitationStatus Status { get; set; }
    }

    public class MonasteryMemberDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public int MonasterySpaceId { get; set; }
        public int SpaceRoleId { get; set; }
        public string? SpaceRoleName { get; set; }
        public bool IsOwner { get; set; }
    }

    public class GetMonasteryMembersRequestDto
    {
        public int MonasterySpaceId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class UpdateMemberRoleRequestDto
    {
        public int MonasterySpaceId { get; set; }
        public int MemberUserId { get; set; }
        public int SpaceRoleId { get; set; }
    }

    public class RemoveMemberRequestDto
    {
        public int MonasterySpaceId { get; set; }
        public int MemberUserId { get; set; }
    }
}
