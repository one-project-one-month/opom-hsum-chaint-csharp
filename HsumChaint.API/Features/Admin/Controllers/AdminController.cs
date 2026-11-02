using System.ComponentModel.DataAnnotations;
using HsumChaint.API.Extensions;
using HsumChaint.Database.Models;
using HsumChaint.Shared;
using HsumChaint.Shared.CommonEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HsumChaint.API.Features.Admin.Controllers;

// System-wide reads are deliberately separate from donor/member-scoped endpoints.
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public partial class AdminController(AppDbContext db) : ControllerBase, IAsyncActionFilter
{
    // A JWT may outlive a role change or account deactivation.
    [NonAction]
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = User.GetCurrentUserId();
        if (!userId.HasValue || !await db.Users.AnyAsync(x => x.Id == userId && x.IsDeleted != true
            && x.Role != null && !x.Role.IsDeleted && x.Role.Name == "Admin", HttpContext.RequestAborted))
        {
            context.Result = Forbid();
            return;
        }
        await next();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var donations = db.DonorLists.AsNoTracking();
        var summary = new
        {
            Donations = await donations.CountAsync(cancellationToken),
            PendingReview = await donations.CountAsync(x => x.StatusValue == DonationStatus.PendingReview, cancellationToken),
            Completed = await donations.CountAsync(x => x.StatusValue == DonationStatus.Completed, cancellationToken),
            Monasteries = await db.MonasterySpaces.CountAsync(x => x.IsDeleted != true, cancellationToken),
            Users = await db.Users.CountAsync(x => x.IsDeleted != true, cancellationToken)
        };
        return Ok(Result<object>.Success(summary));
    }

    [HttpGet("donations")]
    public async Task<IActionResult> Donations([FromQuery] AdminDonationQuery query, CancellationToken cancellationToken)
    {
        if (query.FromDate > query.ToDate)
            return BadRequest(Result.Failure("From date must be before to date."));

        var donations = db.DonorLists.AsNoTracking();
        if (query.CalendarMonth != null)
        {
            if (!DateTime.TryParseExact(query.CalendarMonth + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var month))
                return BadRequest(Result.Failure("Invalid calendar month."));
            var start = new DateTimeOffset(month, TimeSpan.FromHours(6.5)).UtcDateTime;
            var end = new DateTimeOffset(month.AddMonths(1), TimeSpan.FromHours(6.5)).UtcDateTime;
            donations = donations.Where(x => (x.PickupTime ?? x.DropoffTime ?? x.CreatedAt) >= start
                && (x.PickupTime ?? x.DropoffTime ?? x.CreatedAt) < end);
        }
        if (query.Status.HasValue) donations = donations.Where(x => x.StatusValue == query.Status);
        if (query.FromDate.HasValue) donations = donations.Where(x => x.CreatedAt >= query.FromDate);
        if (query.ToDate.HasValue) donations = donations.Where(x => x.CreatedAt <= query.ToDate);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            donations = donations.Where(x => x.DonorName != null && x.DonorName.Contains(search));
        }

        var total = await donations.CountAsync(cancellationToken);
        var rows = await (
            from donation in donations
            join monastery in db.MonasterySpaces.AsNoTracking() on donation.MonasterySpaceId equals monastery.Id into monasteries
            from monastery in monasteries.DefaultIfEmpty()
            orderby donation.CreatedAt descending, donation.Id descending
            select new
            {
                donation.Id, donation.MonasterySpaceId,
                MonasteryName = monastery == null ? null : monastery.MonasteryName,
                donation.DonorName, DonationType = donation.DonationTypeValue,
                donation.CustomDonationType, donation.Amount, donation.Quantity,
                Status = donation.StatusValue, donation.Note, donation.CreatedAt,
                donation.PickupTime, donation.DropoffTime
            })
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize).ToListAsync(cancellationToken);

        return Ok(new { IsSuccess = true, Message = "Success", Data = rows,
            Pagination = new Pagination(query.PageNumber, query.PageSize, total) });
    }
}

public class AdminDonationQuery
{
    [Range(1, 1000000)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(100)] public string? Search { get; set; }
    [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")] public string? CalendarMonth { get; set; }
    [EnumDataType(typeof(DonationStatus))] public DonationStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
