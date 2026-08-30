using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Donation.DTOs;
using HsumChaint.Domain.Features.Donation.ServiceInterfaces;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using DonationEntity = HsumChaint.Database.Models.DonorList;
using NotificationEntity = HsumChaint.Database.Models.Notification;

namespace HsumChaint.Domain.Features.Donation.Services
{
    public class DonationService : IDonationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IFirebaseNotificationProvider _notificationProvider;

        public DonationService(AppDbContext dbContext, IFirebaseNotificationProvider notificationProvider)
        {
            _dbContext = dbContext;
            _notificationProvider = notificationProvider;
        }

        public async Task<ApplicationCommonResponseModel<DonationDto>> RequestDonation(int currentUserId, CreateDonationRequestDto request)
        {
            var response = await ValidateDonationRequest(request);
            if (response != null)
            {
                return response;
            }

            var donor = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUserId && x.IsDeleted == false);
            if (donor == null)
            {
                return Fail("Donor user not found.");
            }

            var donation = new DonationEntity
            {
                MonasterySpaceId = request.MonasterySpaceId,
                DonorId = currentUserId,
                DonorName = donor.Name,
                DonationType = request.DonationType.ToString(),
                DonationTypeValue = request.DonationType,
                CustomDonationType = request.CustomDonationType,
                Note = request.Note,
                Amount = request.Amount,
                Quantity = request.Quantity,
                Status = DonationStatus.PendingReview.ToString(),
                StatusValue = DonationStatus.PendingReview,
                CreatedAt = DateTime.UtcNow,
                PickupTime = request.PickupTime,
                DropoffTime = request.DropoffTime
            };

            await _dbContext.DonorLists.AddAsync(donation);
            await _dbContext.SaveChangesAsync();
            await NotifyMonasteryManagers(request.MonasterySpaceId, "A new donation request is waiting for review.", donation.Id);

            return Success("Donation request submitted successfully.", MapDonation(donation));
        }

        public async Task<ApplicationCommonResponseModel<DonationDto>> CreateManualDonation(int currentUserId, CreateManualDonationRequestDto request)
        {
            var validation = await ValidateDonationRequest(request);
            if (validation != null)
            {
                return validation;
            }

            var member = await GetMember(currentUserId, request.MonasterySpaceId);
            if (!CanManageDonations(member))
            {
                return Fail("User is not authorized to create manual donations for this monastery.");
            }

            var donor = request.DonorId.HasValue
                ? await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.DonorId.Value && x.IsDeleted == false)
                : null;

            if (request.DonorId.HasValue && donor == null)
            {
                return Fail("Donor user not found.");
            }

            if (!request.DonorId.HasValue && string.IsNullOrWhiteSpace(request.DonorName))
            {
                return Fail("Donor name is required for manual donations without a donor account.");
            }

            var initialStatus = request.PickupTime.HasValue || request.DropoffTime.HasValue
                ? DonationStatus.Scheduled
                : DonationStatus.Accepted;

            var donation = new DonationEntity
            {
                MonasterySpaceId = request.MonasterySpaceId,
                DonorId = donor?.Id,
                DonorName = donor?.Name ?? request.DonorName,
                DonationType = request.DonationType.ToString(),
                DonationTypeValue = request.DonationType,
                CustomDonationType = request.CustomDonationType,
                Note = request.Note,
                Amount = request.Amount,
                Quantity = request.Quantity,
                Status = initialStatus.ToString(),
                StatusValue = initialStatus,
                ReviewerId = currentUserId,
                ReviewedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                PickupTime = request.PickupTime,
                DropoffTime = request.DropoffTime
            };

            await _dbContext.DonorLists.AddAsync(donation);
            await _dbContext.SaveChangesAsync();

            if (donation.DonorId.HasValue)
            {
                await NotifyUser(donation.DonorId.Value, "Your donation has been recorded by the monastery.", donation.Id);
            }

            return Success("Manual donation created successfully.", MapDonation(donation));
        }

        public async Task<ApplicationCommonResponseModel<List<DonationDto>>> GetDonations(int currentUserId, DonationQueryDto query)
        {
            var response = new ApplicationCommonResponseModel<List<DonationDto>>();
            IQueryable<DonationEntity> donations = _dbContext.DonorLists.AsNoTracking();

            if (query.MonasterySpaceId.HasValue)
            {
                var member = await GetMember(currentUserId, query.MonasterySpaceId.Value);
                if (member == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User is not a member of this monastery.";
                    return response;
                }

                donations = donations.Where(x => x.MonasterySpaceId == query.MonasterySpaceId.Value);
            }
            else
            {
                donations = donations.Where(x => x.DonorId == currentUserId);
            }

            if (query.DonorId.HasValue)
            {
                if (!query.MonasterySpaceId.HasValue && query.DonorId.Value != currentUserId)
                {
                    response.IsSuccess = false;
                    response.Message = "User is not authorized to view this donor history.";
                    return response;
                }

                donations = donations.Where(x => x.DonorId == query.DonorId.Value);
            }

            if (query.Status.HasValue)
            {
                donations = donations.Where(x => x.StatusValue == query.Status.Value);
            }

            if (query.FromDate.HasValue)
            {
                donations = donations.Where(x => x.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                donations = donations.Where(x => x.CreatedAt <= query.ToDate.Value);
            }

            var donationEntities = await donations
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            var list = donationEntities.Select(MapDonation).ToList();

            response.IsSuccess = true;
            response.Message = list.Count > 0 ? "Donations retrieved successfully." : "Donation list not found.";
            response.ListData = list;
            return response;
        }

        public async Task<ApplicationCommonResponseModel<DonationDto>> GetDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == donationId);
            if (donation == null)
            {
                return Fail("Donation not found.");
            }

            if (!await CanViewDonation(currentUserId, donation))
            {
                return Fail("User is not authorized to view this donation.");
            }

            return Success("Donation retrieved successfully.", MapDonation(donation));
        }

        public async Task<ApplicationCommonResponseModel<DonationDto>> ReviewDonation(int currentUserId, int donationId, ReviewDonationRequestDto request)
        {
            if (request.Status is not (DonationStatus.Accepted or DonationStatus.Rejected))
            {
                return Fail("Review status must be Accepted or Rejected.");
            }

            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Fail("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!CanManageDonations(member))
            {
                return Fail("User is not authorized to review this donation.");
            }

            if (donation.StatusValue is DonationStatus.Completed or DonationStatus.Cancelled)
            {
                return Fail("Completed or cancelled donations cannot be reviewed.");
            }

            donation.StatusValue = request.Status;
            donation.Status = request.Status.ToString();
            donation.ReviewerId = currentUserId;
            donation.ReviewedAt = DateTime.UtcNow;
            donation.Note = string.IsNullOrWhiteSpace(request.Note) ? donation.Note : request.Note;
            await _dbContext.SaveChangesAsync();

            if (donation.DonorId.HasValue)
            {
                await NotifyUser(donation.DonorId.Value, $"Your donation was {request.Status.ToString().ToLower()}.", donation.Id);
            }

            return Success("Donation reviewed successfully.", MapDonation(donation));
        }

        public async Task<ApplicationCommonResponseModel<DonationDto>> ScheduleDonation(int currentUserId, int donationId, ScheduleDonationRequestDto request)
        {
            if (!request.PickupTime.HasValue && !request.DropoffTime.HasValue)
            {
                return Fail("Pickup time or dropoff time is required.");
            }

            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Fail("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!CanScheduleDonations(member))
            {
                return Fail("User is not authorized to schedule this donation.");
            }

            if (donation.StatusValue is DonationStatus.Rejected or DonationStatus.Cancelled or DonationStatus.Completed)
            {
                return Fail("Rejected, cancelled, or completed donations cannot be scheduled.");
            }

            donation.PickupTime = request.PickupTime;
            donation.DropoffTime = request.DropoffTime;
            donation.StatusValue = DonationStatus.Scheduled;
            donation.Status = DonationStatus.Scheduled.ToString();
            await _dbContext.SaveChangesAsync();

            if (donation.DonorId.HasValue)
            {
                await NotifyUser(donation.DonorId.Value, "Your donation pickup/dropoff schedule has been updated.", donation.Id);
            }

            return Success("Donation scheduled successfully.", MapDonation(donation));
        }

        public async Task<ApplicationCommonResponseModel<DonationDto>> CompleteDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Fail("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!CanScheduleDonations(member))
            {
                return Fail("User is not authorized to complete this donation.");
            }

            if (donation.StatusValue is DonationStatus.Rejected or DonationStatus.Cancelled)
            {
                return Fail("Rejected or cancelled donations cannot be completed.");
            }

            donation.StatusValue = DonationStatus.Completed;
            donation.Status = DonationStatus.Completed.ToString();
            donation.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            if (donation.DonorId.HasValue)
            {
                await NotifyUser(donation.DonorId.Value, "Your donation has been completed.", donation.Id);
            }

            return Success("Donation completed successfully.", MapDonation(donation));
        }

        public async Task<ApplicationCommonResponseModel<DonationDto>> CancelDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Fail("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (donation.DonorId != currentUserId && !CanManageDonations(member))
            {
                return Fail("User is not authorized to cancel this donation.");
            }

            if (donation.StatusValue == DonationStatus.Completed)
            {
                return Fail("Completed donations cannot be cancelled.");
            }

            donation.StatusValue = DonationStatus.Cancelled;
            donation.Status = DonationStatus.Cancelled.ToString();
            await _dbContext.SaveChangesAsync();

            if (donation.DonorId.HasValue)
            {
                await NotifyUser(donation.DonorId.Value, "Your donation has been cancelled.", donation.Id);
            }

            return Success("Donation cancelled successfully.", MapDonation(donation));
        }

        private async Task<ApplicationCommonResponseModel<DonationDto>?> ValidateDonationRequest(CreateDonationRequestDto request)
        {
            if (request.MonasterySpaceId <= 0)
            {
                return Fail("Monastery space id is required.");
            }

            var monasteryExists = await _dbContext.MonasterySpaces.AnyAsync(x => x.Id == request.MonasterySpaceId);
            if (!monasteryExists)
            {
                return Fail("Monastery not found.");
            }

            if (request.DonationType == DonationType.Other && string.IsNullOrWhiteSpace(request.CustomDonationType))
            {
                return Fail("Custom donation type is required when donation type is Other.");
            }

            if (request.Amount.HasValue && request.Amount.Value < 0)
            {
                return Fail("Donation amount cannot be negative.");
            }

            if (request.Quantity.HasValue && request.Quantity.Value < 0)
            {
                return Fail("Donation quantity cannot be negative.");
            }

            return null;
        }

        private async Task<bool> CanViewDonation(int currentUserId, DonationEntity donation)
        {
            if (donation.DonorId == currentUserId)
            {
                return true;
            }

            return await GetMember(currentUserId, donation.MonasterySpaceId ?? 0) != null;
        }

        private async Task<MonasteryMember?> GetMember(int userId, int monasterySpaceId)
        {
            return await _dbContext.MonasteryMembers
                .FirstOrDefaultAsync(x => x.UserId == userId && x.MonasterySpaceId == monasterySpaceId);
        }

        private async Task NotifyMonasteryManagers(int monasterySpaceId, string message, int donationId)
        {
            var managerIds = await _dbContext.MonasteryMembers
                .AsNoTracking()
                .Where(x => x.MonasterySpaceId == monasterySpaceId
                    && (x.IsOwner == true || x.Role == MonasteryRole.Owner || x.Role == MonasteryRole.Admin))
                .Select(x => x.UserId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToListAsync();

            foreach (var managerId in managerIds)
            {
                await NotifyUser(managerId, message, donationId);
            }
        }

        private async Task NotifyUser(int userId, string message, int donationId)
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsDeleted == false);
            if (user == null)
            {
                return;
            }

            await _dbContext.Notifications.AddAsync(new NotificationEntity
            {
                UserId = userId,
                Type = NotificationType.Donation,
                Message = message,
                IsRead = false,
                IsDelete = false,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(user.FcmToken))
            {
                await _notificationProvider.SendPushAsync(
                    user.FcmToken,
                    "Donation Update",
                    message,
                    new Dictionary<string, string>
                    {
                        { "donationId", donationId.ToString() },
                        { "type", NotificationType.Donation.ToString() }
                    });
            }
        }

        private static bool CanManageDonations(MonasteryMember? member)
        {
            return member is not null && (member.IsOwner == true || member.Role is MonasteryRole.Owner or MonasteryRole.Admin);
        }

        private static bool CanScheduleDonations(MonasteryMember? member)
        {
            return member is not null && (member.IsOwner == true || member.Role is MonasteryRole.Owner or MonasteryRole.Admin or MonasteryRole.Editor);
        }

        private static DonationDto MapDonation(DonationEntity donation)
        {
            return new DonationDto
            {
                Id = donation.Id,
                MonasterySpaceId = donation.MonasterySpaceId ?? 0,
                DonorId = donation.DonorId,
                DonorName = donation.DonorName,
                DonationType = donation.DonationTypeValue,
                CustomDonationType = donation.CustomDonationType,
                Note = donation.Note,
                Amount = donation.Amount,
                Quantity = donation.Quantity,
                Status = donation.StatusValue,
                ReviewerId = donation.ReviewerId,
                CreatedAt = donation.CreatedAt,
                ReviewedAt = donation.ReviewedAt,
                PickupTime = donation.PickupTime,
                DropoffTime = donation.DropoffTime,
                CompletedAt = donation.CompletedAt
            };
        }

        private static ApplicationCommonResponseModel<DonationDto> Fail(string message)
        {
            return new ApplicationCommonResponseModel<DonationDto>
            {
                IsSuccess = false,
                Message = message
            };
        }

        private static ApplicationCommonResponseModel<DonationDto> Success(string message, DonationDto donation)
        {
            return new ApplicationCommonResponseModel<DonationDto>
            {
                IsSuccess = true,
                Message = message,
                Data = donation
            };
        }
    }
}
