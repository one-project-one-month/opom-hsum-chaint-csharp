using HsumChaint.Shared.CommonEnum;
using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class DonorList
{
    public int Id { get; set; }

    public int? MonasterySpaceId { get; set; }

    public int? DonorId { get; set; }

    public string? DonorName { get; set; }

    public string? DonationType { get; set; }

    public string? Status { get; set; }

    public int? ReviewerId { get; set; }

    public DonationType DonationTypeValue { get; set; }

    public string? CustomDonationType { get; set; }

    public string? Note { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Quantity { get; set; }

    public DonationStatus StatusValue { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? PickupTime { get; set; }

    public DateTime? DropoffTime { get; set; }

    public DateTime? CompletedAt { get; set; }
}
