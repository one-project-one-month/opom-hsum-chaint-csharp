namespace HsumChaint.Domain.Features.User.DTOs;

public class UserSettingDto
{
    public int UserId { get; set; }
    public DateTime? PickupNotificationTime { get; set; }
    public DateTime? DropoffNotificationTime { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? DropoffTime { get; set; }
}

public class UpdateUserSettingRequestDto
{
    public DateTime? PickupNotificationTime { get; set; }
    public DateTime? DropoffNotificationTime { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? DropoffTime { get; set; }
}
