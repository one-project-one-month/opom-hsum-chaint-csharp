using HsumChaint.Shared;
using HsumChaint.Shared.Authorization;
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

        public async Task<Result<DonationDto>> RequestDonation(int currentUserId, CreateDonationRequestDto request)
        {
            var response = await ValidateDonationRequest(request);
            if (response != null)
            {
                return response;
            }

            var donor = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUserId && x.IsDeleted == false);
            if (donor == null)
            {
                return Result<DonationDto>.Failure("Donor user not found.");
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

            return Result<DonationDto>.Success(MapDonation(donation), "Donation request submitted successfully.");
        }

        public async Task<Result<DonationDto>> CreateManualDonation(int currentUserId, CreateManualDonationRequestDto request)
        {
            var validation = await ValidateDonationRequest(request);
            if (validation != null)
            {
                return validation;
            }

            var member = await GetMember(currentUserId, request.MonasterySpaceId);
            if (!await CanManageDonations(member))
            {
                return Result<DonationDto>.Failure("User is not authorized to create manual donations for this monastery.");
            }

            var donor = request.DonorId.HasValue
                ? await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.DonorId.Value && x.IsDeleted == false)
                : null;

            if (request.DonorId.HasValue && donor == null)
            {
                return Result<DonationDto>.Failure("Donor user not found.");
            }

            if (!request.DonorId.HasValue && string.IsNullOrWhiteSpace(request.DonorName))
            {
                return Result<DonationDto>.Failure("Donor name is required for manual donations without a donor account.");
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

            return Result<DonationDto>.Success(MapDonation(donation), "Manual donation created successfully.");
        }

        public async Task<PagedResult<DonationDto>> GetDonations(int currentUserId, DonationQueryDto query)
        {
            var pageNumber = query.PageNumber;
            var pageSize = query.PageSize;
            if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
            {
                return PagedResult<DonationDto>.Failure("Page number and page size must be positive and within the supported range.");
            }
            IQueryable<DonationEntity> donations = _dbContext.DonorLists.AsNoTracking();

            if (query.MonasterySpaceId.HasValue)
            {
                var member = await GetMember(currentUserId, query.MonasterySpaceId.Value);
                if (member == null)
                {
                    return PagedResult<DonationDto>.Failure("User is not a member of this monastery.");
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
                    return PagedResult<DonationDto>.Failure("User is not authorized to view this donor history.");
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

            var totalCount = await donations.CountAsync();
            var donationEntities = await donations
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var list = donationEntities.Select(MapDonation).ToList();

            return PagedResult<DonationDto>.Success(list, new Pagination(pageNumber, pageSize, totalCount), "Donations retrieved successfully.");
        }

        public async Task<Result<DonationDto>> GetDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == donationId);
            if (donation == null)
            {
                return Result<DonationDto>.Failure("Donation not found.");
            }

            if (!await CanViewDonation(currentUserId, donation))
            {
                return Result<DonationDto>.Failure("User is not authorized to view this donation.");
            }

            return Result<DonationDto>.Success(MapDonation(donation), "Donation retrieved successfully.");
        }

        public async Task<Result<DonationDto>> ReviewDonation(int currentUserId, int donationId, ReviewDonationRequestDto request)
        {
            if (request.Status is not (DonationStatus.Accepted or DonationStatus.Rejected))
            {
                return Result<DonationDto>.Failure("Review status must be Accepted or Rejected.");
            }

            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Result<DonationDto>.Failure("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!await CanManageDonations(member))
            {
                return Result<DonationDto>.Failure("User is not authorized to review this donation.");
            }

            if (donation.StatusValue is DonationStatus.Completed or DonationStatus.Cancelled)
            {
                return Result<DonationDto>.Failure("Completed or cancelled donations cannot be reviewed.");
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

            return Result<DonationDto>.Success(MapDonation(donation), "Donation reviewed successfully.");
        }

        public async Task<Result<DonationDto>> ScheduleDonation(int currentUserId, int donationId, ScheduleDonationRequestDto request)
        {
            if (!request.PickupTime.HasValue && !request.DropoffTime.HasValue)
            {
                return Result<DonationDto>.Failure("Pickup time or dropoff time is required.");
            }

            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Result<DonationDto>.Failure("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!await CanScheduleDonations(member))
            {
                return Result<DonationDto>.Failure("User is not authorized to schedule this donation.");
            }

            if (donation.StatusValue is DonationStatus.Rejected or DonationStatus.Cancelled or DonationStatus.Completed)
            {
                return Result<DonationDto>.Failure("Rejected, cancelled, or completed donations cannot be scheduled.");
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

            return Result<DonationDto>.Success(MapDonation(donation), "Donation scheduled successfully.");
        }

        public async Task<Result<DonationDto>> CompleteDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Result<DonationDto>.Failure("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!await CanScheduleDonations(member))
            {
                return Result<DonationDto>.Failure("User is not authorized to complete this donation.");
            }

            if (donation.StatusValue is DonationStatus.Rejected or DonationStatus.Cancelled)
            {
                return Result<DonationDto>.Failure("Rejected or cancelled donations cannot be completed.");
            }

            donation.StatusValue = DonationStatus.Completed;
            donation.Status = DonationStatus.Completed.ToString();
            donation.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            if (donation.DonorId.HasValue)
            {
                await NotifyUser(donation.DonorId.Value, "Your donation has been completed.", donation.Id);
            }

            return Result<DonationDto>.Success(MapDonation(donation), "Donation completed successfully.");
        }

        public async Task<Result<DonationDto>> CancelDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Result<DonationDto>.Failure("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (donation.DonorId != currentUserId && !await CanManageDonations(member))
            {
                return Result<DonationDto>.Failure("User is not authorized to cancel this donation.");
            }

            if (donation.StatusValue == DonationStatus.Completed)
            {
                return Result<DonationDto>.Failure("Completed donations cannot be cancelled.");
            }

            donation.StatusValue = DonationStatus.Cancelled;
            donation.Status = DonationStatus.Cancelled.ToString();
            await _dbContext.SaveChangesAsync();

            if (donation.DonorId.HasValue)
            {
                await NotifyUser(donation.DonorId.Value, "Your donation has been cancelled.", donation.Id);
            }

            return Result<DonationDto>.Success(MapDonation(donation), "Donation cancelled successfully.");
        }

        private async Task<Result<DonationDto>?> ValidateDonationRequest(CreateDonationRequestDto request)
        {
            if (request.MonasterySpaceId <= 0)
            {
                return Result<DonationDto>.Failure("Monastery space id is required.");
            }

            var monasteryExists = await _dbContext.MonasterySpaces.AnyAsync(x => x.Id == request.MonasterySpaceId);
            if (!monasteryExists)
            {
                return Result<DonationDto>.Failure("Monastery not found.");
            }

            if (request.DonationType == DonationType.Other && string.IsNullOrWhiteSpace(request.CustomDonationType))
            {
                return Result<DonationDto>.Failure("Custom donation type is required when donation type is Other.");
            }

            if (request.Amount.HasValue && request.Amount.Value < 0)
            {
                return Result<DonationDto>.Failure("Donation amount cannot be negative.");
            }

            if (request.Quantity.HasValue && request.Quantity.Value < 0)
            {
                return Result<DonationDto>.Failure("Donation quantity cannot be negative.");
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
                    && (x.IsOwner == true || _dbContext.RolePermissions.Any(rp => rp.RoleId == x.RoleId && !rp.IsDeleted && !rp.Role.IsDeleted && !rp.Permission.IsDeleted && rp.Permission.Name == Permissions.Donation.Review)))
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

        private async Task<bool> CanManageDonations(MonasteryMember? member)
        {
            return member is not null && (member.IsOwner == true || await HasRolePermission(member.RoleId, Permissions.Donation.Review));
        }

        private async Task<bool> CanScheduleDonations(MonasteryMember? member)
        {
            return member is not null && (member.IsOwner == true || await HasRolePermission(member.RoleId, Permissions.Donation.Schedule));
        }

        private Task<bool> HasRolePermission(int roleId, string permission) => _dbContext.RolePermissions.AnyAsync(rp =>
            rp.RoleId == roleId && !rp.IsDeleted && !rp.Role.IsDeleted && !rp.Permission.IsDeleted && rp.Permission.Name == permission);

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

    }
}
