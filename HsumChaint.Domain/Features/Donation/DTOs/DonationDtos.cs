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

    public class ReviewDonationRequestDto
    {
        public DonationStatus Status { get; set; }
        public string? Note { get; set; }
    }

    public class ScheduleDonationRequestDto
    {
        public DateTime? PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }
    }

    public class DonationQueryDto
    {
        public int? MonasterySpaceId { get; set; }
        public int? DonorId { get; set; }
        public DonationStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
