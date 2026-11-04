using HsumChaint.Shared;
using HsumChaint.Domain.Features.Donation.DTOs;

namespace HsumChaint.Domain.Features.Donation.ServiceInterfaces
{
    public interface IDonationService
    {
        Task<Result<DonationDto>> RequestDonation(int currentUserId, CreateDonationRequestDto request);
        Task<Result<DonationDto>> CreateManualDonation(int currentUserId, CreateManualDonationRequestDto request);
        Task<PagedResult<DonationDto>> GetDonations(int currentUserId, DonationQueryDto query);
        Task<CursorPagedResult<DonationDto>> GetDonationsFeed(int currentUserId, DonationFeedQueryDto query);
        Task<Result<DonationDto>> GetDonation(int currentUserId, GetDonationRequestDto request);
        Task<Result<DonationDto>> ReviewDonation(int currentUserId, ReviewDonationRequestDto request);
        Task<Result<DonationDto>> ScheduleDonation(int currentUserId, ScheduleDonationRequestDto request);
        Task<Result<DonationDto>> CompleteDonation(int currentUserId, CompleteDonationRequestDto request);
        Task<Result<DonationDto>> CancelDonation(int currentUserId, CancelDonationRequestDto request);
        Task<Result<List<DonationDto>>> CreateBatchManualDonations(int currentUserId, CreateBatchDonationRequestDto request);
        Task<Result<List<DonationDto>>> RequestRecurringDonation(int currentUserId, CreateRecurringDonationRequestDto request);
        Task<Result<List<DonationDto>>> CreateManualRecurringDonation(int currentUserId, CreateManualRecurringDonationRequestDto request);
        Task<Result<bool>> DeleteDonation(int currentUserId, DeleteDonationRequestDto request);
        Task<Result<MonthlyDonationSummaryDto>> GetMonthlyCalendar(int currentUserId, MonthlyCalendarQueryDto query);
    }
}
