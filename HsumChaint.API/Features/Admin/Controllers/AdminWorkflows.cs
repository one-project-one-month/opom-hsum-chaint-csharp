using System.ComponentModel.DataAnnotations;
using HsumChaint.API.Extensions;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Domain.Features.User.DTOs;
using HsumChaint.Domain.Features.User.ServiceInterfaces;
using HsumChaint.Shared;
using HsumChaint.Shared.CommonEnum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HsumChaint.API.Features.Admin.Controllers;

public partial class AdminController
{
    [HttpGet("monasteries/{id:int}")]
    public async Task<IActionResult> Monastery(int id)
    {
        var monastery = await db.MonasterySpaces.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (monastery == null) return NotFound(Result.Failure("Monastery not found."));
        var isOwner = await db.MonasteryMembers.AnyAsync(x => x.MonasterySpaceId == id && x.UserId == User.GetCurrentUserId() && x.IsOwner == true);
        return Ok(Result<object>.Success(new { monastery.Id, monastery.MonasteryName, monastery.Address, monastery.Description, monastery.CreatedById, IsOwner = isOwner }));
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(AdminCreateUser request, [FromServices] IAuthService service)
        => (await service.Register(request)).ToActionResult();

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UserDto request, [FromServices] IUserService service)
    {
        var existing = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted == false);
        if (existing == null) return NotFound(Result.Failure("User not found."));
        if (await db.Users.AnyAsync(u => u.Id != id && u.IsDeleted == false && u.PhoneNumber == request.PhoneNumber))
            return BadRequest(Result.Failure("Phone number already exists."));
        if (id == User.GetCurrentUserId() && request.RoleId != existing.RoleId)
            return BadRequest(Result.Failure("You cannot change your own system role."));
        if (existing.Role?.Name == "Admin" && request.RoleId != existing.RoleId
            && await db.Users.CountAsync(u => u.IsDeleted == false && u.Role != null && u.Role.Name == "Admin" && !u.Role.IsDeleted) <= 1)
            return BadRequest(Result.Failure("The last active Admin cannot be demoted."));
        request.Id = id;
        return (await service.PutUser(request)).ToActionResult();
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeactivateUser(int id, [FromServices] IUserService service)
    {
        if (id == User.GetCurrentUserId()) return BadRequest(Result.Failure("You cannot deactivate your own account."));
        if (await db.MonasteryMembers.AnyAsync(m => m.UserId == id && m.IsOwner == true && m.MonasterySpace != null && !m.MonasterySpace.IsDeleted))
            return BadRequest(Result.Failure("A monastery owner cannot be deactivated while owning an active monastery."));
        return (await service.DeleteUser(new DeleteUserRequestDto { Id = id })).ToActionResult();
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> Invitations([FromQuery] AdminPageQuery query)
    {
        var rows = db.Invitations.AsNoTracking();
        var count = await rows.CountAsync();
        var data = await rows.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new { x.Id, x.MonasterySpaceId, MonasteryName = x.MonasterySpace == null ? null : x.MonasterySpace.MonasteryName,
                x.InvitedUserId, UserName = x.InvitedUser == null ? null : x.InvitedUser.Name,
                x.InvitedById, x.SpaceRoleId, SpaceRoleName = x.SpaceRole == null ? null : x.SpaceRole.Name, x.Status, x.CreatedAt }).ToListAsync();
        return Ok(new { IsSuccess = true, Message = "Success", Data = data, Pagination = new Pagination(query.PageNumber, query.PageSize, count) });
    }

    [HttpDelete("invitations/{id:int}")]
    public async Task<IActionResult> RevokeInvitation(int id)
    {
        var invitation = await db.Invitations.FindAsync(id);
        if (invitation == null) return NotFound(Result.Failure("Invitation not found."));
        if (invitation.Status != InvitationStatus.Pending) return BadRequest(Result.Failure("Only pending invitations can be revoked."));
        // Preserve the invitation as an audit record; no membership is created.
        invitation.Status = InvitationStatus.Reject;
        await db.SaveChangesAsync();
        return Ok(Result.Success("Invitation revoked."));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromQuery] AdminPageQuery query)
    {
        var rows = db.Notifications.AsNoTracking().Where(n => n.IsDeleted != true);
        var count = await rows.CountAsync();
        var data = await (from n in rows
            join u in db.Users on n.UserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            orderby n.CreatedAt descending, n.Id descending
            select new { n.Id, n.UserId, UserName = u == null ? null : u.Name, n.Message, n.Type, n.IsRead, n.CreatedAt })
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
        return Ok(new { IsSuccess = true, Message = "Success", Data = data, Pagination = new Pagination(query.PageNumber, query.PageSize, count) });
    }

    [HttpPost("notifications")]
    public async Task<IActionResult> CreateNotification(AdminNotification request)
    {
        if (!await db.Users.AnyAsync(u => u.Id == request.UserId && u.IsDeleted == false))
            return NotFound(Result.Failure("User not found."));
        // Admin announcements are database notifications. Push delivery is not implied.
        db.Notifications.Add(new HsumChaint.Database.Models.Notification { UserId = request.UserId, Type = request.Type,
            Message = request.Message.Trim(), IsRead = false, IsDeleted = false, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return Ok(Result.Success("Notification created."));
    }

    [HttpPatch("notifications/{id:int}/read")]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var row = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.IsDeleted != true);
        if (row == null) return NotFound(Result.Failure("Notification not found."));
        row.IsRead = true;
        await db.SaveChangesAsync();
        return Ok(Result.Success());
    }

    [HttpDelete("notifications/{id:int}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var row = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.IsDeleted != true);
        if (row == null) return NotFound(Result.Failure("Notification not found."));
        row.IsDeleted = true;
        await db.SaveChangesAsync();
        return Ok(Result.Success());
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> ReadAllNotifications()
    {
        var rows = await db.Notifications.Where(n => n.IsDeleted != true && n.IsRead != true).ToListAsync();
        foreach (var row in rows) row.IsRead = true;
        await db.SaveChangesAsync();
        return Ok(Result.Success());
    }

    [HttpDelete("notifications")]
    public async Task<IActionResult> ClearNotifications()
    {
        var rows = await db.Notifications.Where(n => n.IsDeleted != true).ToListAsync();
        foreach (var row in rows) row.IsDeleted = true;
        await db.SaveChangesAsync();
        return Ok(Result.Success("All notifications cleared."));
    }
}

public class AdminPageQuery
{
    [Range(1, 1000000)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
}

public class AdminNotification
{
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    [Required, StringLength(2000)] public string Message { get; set; } = "";
    [EnumDataType(typeof(NotificationType))] public NotificationType Type { get; set; }
}

public class AdminCreateUser : RegisterRequestDto
{
    // Register performs required-field, duplicate-phone and active-role checks.
}
