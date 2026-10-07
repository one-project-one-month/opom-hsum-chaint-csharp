using HsumChaint.Shared;
using HsumChaint.Domain.Features.Donation.DTOs;

namespace HsumChaint.Domain.Features.Donation.ServiceInterfaces
{
    public interface IDonationService
    {
        Task<Result<DonationDto>> RequestDonation(int currentUserId, CreateDonationRequestDto request);
        Task<Result<DonationDto>> CreateManualDonation(int currentUserId, CreateManualDonationRequestDto request);
        Task<PagedResult<DonationDto>> GetDonations(int currentUserId, DonationQueryDto query);
        Task<Result<DonationDto>> GetDonation(int currentUserId, int donationId);
        Task<Result<DonationDto>> ReviewDonation(int currentUserId, int donationId, ReviewDonationRequestDto request);
        Task<Result<DonationDto>> ScheduleDonation(int currentUserId, int donationId, ScheduleDonationRequestDto request);
        Task<Result<DonationDto>> CompleteDonation(int currentUserId, int donationId);
        Task<Result<DonationDto>> CancelDonation(int currentUserId, int donationId);
    }
}
