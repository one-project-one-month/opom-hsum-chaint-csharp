using HsumChaint.Domain.Features.Donation.DTOs;

namespace HsumChaint.Domain.Features.Donation.ServiceInterfaces
{
    public interface IDonationService
    {
        Task<ApplicationCommonResponseModel<DonationDto>> RequestDonation(int currentUserId, CreateDonationRequestDto request);
        Task<ApplicationCommonResponseModel<DonationDto>> CreateManualDonation(int currentUserId, CreateManualDonationRequestDto request);
        Task<ApplicationCommonResponseModel<List<DonationDto>>> GetDonations(int currentUserId, DonationQueryDto query);
        Task<ApplicationCommonResponseModel<DonationDto>> GetDonation(int currentUserId, int donationId);
        Task<ApplicationCommonResponseModel<DonationDto>> ReviewDonation(int currentUserId, int donationId, ReviewDonationRequestDto request);
        Task<ApplicationCommonResponseModel<DonationDto>> ScheduleDonation(int currentUserId, int donationId, ScheduleDonationRequestDto request);
        Task<ApplicationCommonResponseModel<DonationDto>> CompleteDonation(int currentUserId, int donationId);
        Task<ApplicationCommonResponseModel<DonationDto>> CancelDonation(int currentUserId, int donationId);
    }
}
