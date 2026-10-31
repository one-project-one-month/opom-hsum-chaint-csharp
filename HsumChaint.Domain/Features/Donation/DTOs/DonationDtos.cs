using HsumChaint.Shared;
using HsumChaint.Shared.CommonEnum;

namespace HsumChaint.Domain.Features.Donation.DTOs
{
    public class CreateDonationRequestDto
    {
        public int MonasterySpaceId { get; set; }
        public DonationType DonationType { get; set; }
        public string? CustomDonationType { get; set; }
        public string? Note { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Quantity { get; set; }
        public DateTime? PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }
    }

    public class CreateManualDonationRequestDto : CreateDonationRequestDto
    {
        public int? DonorId { get; set; }
        public string? DonorName { get; set; }
    }

    public class DonationDto
    {
        public int Id { get; set; }
        public int MonasterySpaceId { get; set; }
        public int? DonorId { get; set; }
        public string? DonorName { get; set; }
        public DonationType DonationType { get; set; }
        public string? CustomDonationType { get; set; }
        public string? Note { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Quantity { get; set; }
        public DonationStatus Status { get; set; }
        public int? ReviewerId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class GetDonationRequestDto
    {
        public int Id { get; set; }
    }

    public class ReviewDonationRequestDto
    {
        public int Id { get; set; }
        public DonationStatus Status { get; set; }
        public string? Note { get; set; }
    }

    public class ScheduleDonationRequestDto
    {
        public int Id { get; set; }
        public DateTime? PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }
    }

    public class CompleteDonationRequestDto
    {
        public int Id { get; set; }
    }

    public class CancelDonationRequestDto
    {
        public int Id { get; set; }
    }

    public class DeleteDonationRequestDto
    {
        public int Id { get; set; }
    }

    public class DonationQueryDto : PaginationRequest
    {
        public int? MonasterySpaceId { get; set; }
        public int? DonorId { get; set; }
        public DonationStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class CreateBatchDonationRequestDto
    {
        public int MonasterySpaceId { get; set; }
        public List<CreateManualDonationRequestDto> Donations { get; set; } = new();
    }

    public class CreateRecurringDonationRequestDto
    {
        // Optional for backward compatibility; existing clients use UTC (zero).
        public int TimeZoneOffsetMinutes { get; set; }
        public int MonasterySpaceId { get; set; }
        public DonationType DonationType { get; set; }
        public string? CustomDonationType { get; set; }
        public string? Note { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Quantity { get; set; }
        public DateTime StartDate { get; set; }
        public int DayOfMonth { get; set; }
        public int MonthsCount { get; set; }
        public TimeSpan? PickupTimeOfDay { get; set; }
        public TimeSpan? DropoffTimeOfDay { get; set; }
    }

    public class CreateManualRecurringDonationRequestDto : CreateRecurringDonationRequestDto
    {
        public int? DonorId { get; set; }
        public string? DonorName { get; set; }
    }

    public class MonthlyCalendarQueryDto
    {
        public int? MonasterySpaceId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DonationStatus? Status { get; set; }
    }

    public class MonthlyDonationSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalDonationsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<DailyDonationSummaryDto> Days { get; set; } = new();
    }

    public class DailyDonationSummaryDto
    {
        public int Day { get; set; }
        public string Date { get; set; } = null!;
        public int DonationsCount { get; set; }
        public List<DonationDto> Donations { get; set; } = new();
    }
}
