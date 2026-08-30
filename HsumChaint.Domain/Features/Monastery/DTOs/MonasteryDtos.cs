using HsumChaint.Shared.CommonEnum;

namespace HsumChaint.Domain.Features.Monastery.DTOs
{
    public class CreateMonasteryRequestDto
    {
        public string? MonasteryName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
    }

    public class UpdateMonasteryRequestDto
    {
        public string? MonasteryName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
    }

    public class MonasterySpaceDto
    {
        public int Id { get; set; }
        public string? MonasteryName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public int? CreatedById { get; set; }
        public MonasteryRole? CurrentUserRole { get; set; }
        public bool IsOwner { get; set; }
    }

    public class InviteMemberRequestDto
    {
        public int? UserId { get; set; }
        public string? PhoneNumber { get; set; }
        public MonasteryRole Role { get; set; } = MonasteryRole.Viewer;
    }

    public class InvitationResponseDto
    {
        public int InvitationId { get; set; }
        public int MonasterySpaceId { get; set; }
        public int InvitedUserId { get; set; }
        public int InvitedById { get; set; }
        public MonasteryRole Role { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class RespondInvitationRequestDto
    {
        public InvitationStatus Status { get; set; }
    }

    public class MonasteryMemberDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public int MonasterySpaceId { get; set; }
        public MonasteryRole Role { get; set; }
        public bool IsOwner { get; set; }
    }

    public class UpdateMemberRoleRequestDto
    {
        public MonasteryRole Role { get; set; }
    }
}
