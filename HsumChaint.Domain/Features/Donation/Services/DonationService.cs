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
            if (!await IsSystemAdmin(currentUserId) && !await CanManageDonations(member))
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
                if (!await _dbContext.MonasterySpaces.AnyAsync(m => m.Id == query.MonasterySpaceId.Value && !m.IsDeleted))
                    return PagedResult<DonationDto>.Failure("Monastery not found.");
                var member = await GetMember(currentUserId, query.MonasterySpaceId.Value);
                if (member == null && !await IsSystemAdmin(currentUserId))
                {
                    return PagedResult<DonationDto>.Failure("User is not a member of this monastery.");
                }

                donations = donations.Where(x => x.MonasterySpaceId == query.MonasterySpaceId.Value);
            }
            else
            {
                if (!await IsSystemAdmin(currentUserId)) donations = donations.Where(x => x.DonorId == currentUserId);
            }

            if (query.DonorId.HasValue)
            {
                if (!query.MonasterySpaceId.HasValue && query.DonorId.Value != currentUserId && !await IsSystemAdmin(currentUserId))
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
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var list = donationEntities.Select(MapDonation).ToList();

            return PagedResult<DonationDto>.Success(list, new Pagination(pageNumber, pageSize, totalCount), "Donations retrieved successfully.");
        }

        public Task<Result<DonationDto>> GetDonation(int currentUserId, GetDonationRequestDto request)
        {
            return GetDonation(currentUserId, request.Id);
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

        public Task<Result<DonationDto>> ReviewDonation(int currentUserId, ReviewDonationRequestDto request)
        {
            return ReviewDonation(currentUserId, request.Id, request);
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
            if (!await IsSystemAdmin(currentUserId) && !await CanManageDonations(member))
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

        public Task<Result<DonationDto>> ScheduleDonation(int currentUserId, ScheduleDonationRequestDto request)
        {
            return ScheduleDonation(currentUserId, request.Id, request);
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
            if (!await IsSystemAdmin(currentUserId) && !await CanScheduleDonations(member))
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

        public Task<Result<DonationDto>> CompleteDonation(int currentUserId, CompleteDonationRequestDto request)
        {
            return CompleteDonation(currentUserId, request.Id);
        }

        public async Task<Result<DonationDto>> CompleteDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Result<DonationDto>.Failure("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!await IsSystemAdmin(currentUserId) && !await CanScheduleDonations(member))
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

        public Task<Result<DonationDto>> CancelDonation(int currentUserId, CancelDonationRequestDto request)
        {
            return CancelDonation(currentUserId, request.Id);
        }

        public async Task<Result<DonationDto>> CancelDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Result<DonationDto>.Failure("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (donation.DonorId != currentUserId && !await IsSystemAdmin(currentUserId) && !await CanManageDonations(member))
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

        public async Task<Result<List<DonationDto>>> CreateBatchManualDonations(int currentUserId, CreateBatchDonationRequestDto request)
        {
            if (request.MonasterySpaceId <= 0)
            {
                return Result<List<DonationDto>>.Failure("Monastery space id is required.");
            }

            if (request.Donations == null || request.Donations.Count == 0)
            {
                return Result<List<DonationDto>>.Failure("At least one donation must be provided.");
            }

            if (request.Donations.Count > 50)
            {
                return Result<List<DonationDto>>.Failure("Batch size cannot exceed 50 donations.");
            }

            var member = await GetMember(currentUserId, request.MonasterySpaceId);
            if (!await IsSystemAdmin(currentUserId) && !await CanManageDonations(member))
            {
                return Result<List<DonationDto>>.Failure("User is not authorized to create donations for this monastery.");
            }

            var entities = new List<DonationEntity>();
            var now = DateTime.UtcNow;

            foreach (var item in request.Donations)
            {
                item.MonasterySpaceId = request.MonasterySpaceId;
                var validation = await ValidateDonationRequest(item);
                if (validation != null)
                {
                    return Result<List<DonationDto>>.Failure(validation.Message);
                }

                var donor = item.DonorId.HasValue
                    ? await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == item.DonorId.Value && x.IsDeleted == false)
                    : null;

                if (item.DonorId.HasValue && donor == null)
                {
                    return Result<List<DonationDto>>.Failure($"Donor user with id {item.DonorId.Value} not found.");
                }

                if (!item.DonorId.HasValue && string.IsNullOrWhiteSpace(item.DonorName))
                {
                    return Result<List<DonationDto>>.Failure("Donor name is required for manual donations without a donor account.");
                }

                var initialStatus = item.PickupTime.HasValue || item.DropoffTime.HasValue
                    ? DonationStatus.Scheduled
                    : DonationStatus.Accepted;

                var donation = new DonationEntity
                {
                    MonasterySpaceId = request.MonasterySpaceId,
                    DonorId = donor?.Id,
                    DonorName = donor?.Name ?? item.DonorName,
                    DonationType = item.DonationType.ToString(),
                    DonationTypeValue = item.DonationType,
                    CustomDonationType = item.CustomDonationType,
                    Note = item.Note,
                    Amount = item.Amount,
                    Quantity = item.Quantity,
                    Status = initialStatus.ToString(),
                    StatusValue = initialStatus,
                    ReviewerId = currentUserId,
                    ReviewedAt = now,
                    CreatedAt = now,
                    PickupTime = item.PickupTime,
                    DropoffTime = item.DropoffTime
                };

                entities.Add(donation);
            }

            await _dbContext.DonorLists.AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();

            foreach (var d in entities)
            {
                if (d.DonorId.HasValue)
                {
                    await NotifyUser(d.DonorId.Value, "Your donation has been recorded by the monastery.", d.Id);
                }
            }

            var resultList = entities.Select(MapDonation).ToList();
            return Result<List<DonationDto>>.Success(resultList, $"{resultList.Count} donations created successfully in batch.");
        }

        public async Task<Result<List<DonationDto>>> RequestRecurringDonation(int currentUserId, CreateRecurringDonationRequestDto request)
        {
            var validation = await ValidateRecurringRequest(request);
            if (validation != null)
            {
                return Result<List<DonationDto>>.Failure(validation);
            }

            var donor = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUserId && x.IsDeleted == false);
            if (donor == null)
            {
                return Result<List<DonationDto>>.Failure("Donor user not found.");
            }

            var seriesTag = Guid.NewGuid().ToString("N")[..8];
            var entities = new List<DonationEntity>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < request.MonthsCount; i++)
            {
                var targetMonth = request.StartDate.AddMonths(i);
                var clampedDay = Math.Min(request.DayOfMonth, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));
                var targetDate = new DateTime(targetMonth.Year, targetMonth.Month, clampedDay, 0, 0, 0, DateTimeKind.Utc);

                DateTime? pickup = request.PickupTimeOfDay.HasValue ? targetDate.Add(request.PickupTimeOfDay.Value).AddMinutes(-request.TimeZoneOffsetMinutes) : null;
                DateTime? dropoff = request.DropoffTimeOfDay.HasValue ? targetDate.Add(request.DropoffTimeOfDay.Value).AddMinutes(-request.TimeZoneOffsetMinutes) : null;

                var seriesNote = string.IsNullOrWhiteSpace(request.Note)
                    ? $"[Series: {seriesTag}] ({i + 1}/{request.MonthsCount})"
                    : $"[Series: {seriesTag}] ({i + 1}/{request.MonthsCount}) - {request.Note}";

                var donation = new DonationEntity
                {
                    MonasterySpaceId = request.MonasterySpaceId,
                    DonorId = currentUserId,
                    DonorName = donor.Name,
                    DonationType = request.DonationType.ToString(),
                    DonationTypeValue = request.DonationType,
                    CustomDonationType = request.CustomDonationType,
                    Note = seriesNote,
                    Amount = request.Amount,
                    Quantity = request.Quantity,
                    Status = DonationStatus.PendingReview.ToString(),
                    StatusValue = DonationStatus.PendingReview,
                    CreatedAt = now,
                    PickupTime = pickup,
                    DropoffTime = dropoff
                };

                entities.Add(donation);
            }

            await _dbContext.DonorLists.AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();

            await NotifyMonasteryManagers(request.MonasterySpaceId, $"New recurring donation series ({request.MonthsCount} months) submitted for review.", entities[0].Id);

            var resultList = entities.Select(MapDonation).ToList();
            return Result<List<DonationDto>>.Success(resultList, $"Recurring donation series of {resultList.Count} donations requested successfully.");
        }

        public async Task<Result<List<DonationDto>>> CreateManualRecurringDonation(int currentUserId, CreateManualRecurringDonationRequestDto request)
        {
            var validation = await ValidateRecurringRequest(request);
            if (validation != null)
            {
                return Result<List<DonationDto>>.Failure(validation);
            }

            var member = await GetMember(currentUserId, request.MonasterySpaceId);
            if (!await IsSystemAdmin(currentUserId) && !await CanManageDonations(member))
            {
                return Result<List<DonationDto>>.Failure("User is not authorized to create manual donations for this monastery.");
            }

            var donor = request.DonorId.HasValue
                ? await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.DonorId.Value && x.IsDeleted == false)
                : null;

            if (request.DonorId.HasValue && donor == null)
            {
                return Result<List<DonationDto>>.Failure("Donor user not found.");
            }

            if (!request.DonorId.HasValue && string.IsNullOrWhiteSpace(request.DonorName))
            {
                return Result<List<DonationDto>>.Failure("Donor name is required for manual donations without a donor account.");
            }

            var seriesTag = Guid.NewGuid().ToString("N")[..8];
            var entities = new List<DonationEntity>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < request.MonthsCount; i++)
            {
                var targetMonth = request.StartDate.AddMonths(i);
                var clampedDay = Math.Min(request.DayOfMonth, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));
                var targetDate = new DateTime(targetMonth.Year, targetMonth.Month, clampedDay, 0, 0, 0, DateTimeKind.Utc);

                DateTime? pickup = request.PickupTimeOfDay.HasValue ? targetDate.Add(request.PickupTimeOfDay.Value).AddMinutes(-request.TimeZoneOffsetMinutes) : null;
                DateTime? dropoff = request.DropoffTimeOfDay.HasValue ? targetDate.Add(request.DropoffTimeOfDay.Value).AddMinutes(-request.TimeZoneOffsetMinutes) : null;

                var initialStatus = pickup.HasValue || dropoff.HasValue ? DonationStatus.Scheduled : DonationStatus.Accepted;

                var seriesNote = string.IsNullOrWhiteSpace(request.Note)
                    ? $"[Series: {seriesTag}] ({i + 1}/{request.MonthsCount})"
                    : $"[Series: {seriesTag}] ({i + 1}/{request.MonthsCount}) - {request.Note}";

                var donation = new DonationEntity
                {
                    MonasterySpaceId = request.MonasterySpaceId,
                    DonorId = donor?.Id,
                    DonorName = donor?.Name ?? request.DonorName,
                    DonationType = request.DonationType.ToString(),
                    DonationTypeValue = request.DonationType,
                    CustomDonationType = request.CustomDonationType,
                    Note = seriesNote,
                    Amount = request.Amount,
                    Quantity = request.Quantity,
                    Status = initialStatus.ToString(),
                    StatusValue = initialStatus,
                    ReviewerId = currentUserId,
                    ReviewedAt = now,
                    CreatedAt = now,
                    PickupTime = pickup,
                    DropoffTime = dropoff
                };

                entities.Add(donation);
            }

            await _dbContext.DonorLists.AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();

            if (donor != null)
            {
                await NotifyUser(donor.Id, $"A recurring donation series ({request.MonthsCount} months) was recorded for you.", entities[0].Id);
            }

            var resultList = entities.Select(MapDonation).ToList();
            return Result<List<DonationDto>>.Success(resultList, $"Recurring donation series of {resultList.Count} donations created successfully.");
        }

        public Task<Result<bool>> DeleteDonation(int currentUserId, DeleteDonationRequestDto request)
        {
            return DeleteDonation(currentUserId, request.Id);
        }

        public async Task<Result<bool>> DeleteDonation(int currentUserId, int donationId)
        {
            var donation = await _dbContext.DonorLists.FindAsync(donationId);
            if (donation == null)
            {
                return Result<bool>.Failure("Donation not found.");
            }

            var member = await GetMember(currentUserId, donation.MonasterySpaceId ?? 0);
            if (!await IsSystemAdmin(currentUserId) && !await CanManageDonations(member))
            {
                return Result<bool>.Failure("User is not authorized to delete donations in this monastery.");
            }

            if (donation.StatusValue == DonationStatus.Completed)
            {
                return Result<bool>.Failure("Completed donations cannot be deleted.");
            }

            _dbContext.DonorLists.Remove(donation);
            await _dbContext.SaveChangesAsync();

            return Result<bool>.Success(true, "Donation deleted successfully.");
        }

        public async Task<Result<MonthlyDonationSummaryDto>> GetMonthlyCalendar(int currentUserId, MonthlyCalendarQueryDto query)
        {
            if (query.Year < 2000 || query.Year > 2100)
            {
                return Result<MonthlyDonationSummaryDto>.Failure("Invalid year.");
            }

            if (query.Month < 1 || query.Month > 12)
            {
                return Result<MonthlyDonationSummaryDto>.Failure("Invalid month.");
            }

            var startOfMonth = new DateTime(query.Year, query.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            var donationsQuery = _dbContext.DonorLists.AsNoTracking();

            if (query.MonasterySpaceId.HasValue)
            {
                if (!await _dbContext.MonasterySpaces.AnyAsync(m => m.Id == query.MonasterySpaceId.Value && !m.IsDeleted))
                    return Result<MonthlyDonationSummaryDto>.Failure("Monastery not found.");
                var member = await GetMember(currentUserId, query.MonasterySpaceId.Value);
                if (member == null && !await IsSystemAdmin(currentUserId))
                {
                    return Result<MonthlyDonationSummaryDto>.Failure("User is not a member of this monastery.");
                }
                donationsQuery = donationsQuery.Where(x => x.MonasterySpaceId == query.MonasterySpaceId.Value);
            }
            else
            {
                if (!await IsSystemAdmin(currentUserId)) donationsQuery = donationsQuery.Where(x => x.DonorId == currentUserId);
            }

            if (query.Status.HasValue)
            {
                donationsQuery = donationsQuery.Where(x => x.StatusValue == query.Status.Value);
            }

            donationsQuery = donationsQuery.Where(x =>
                (x.PickupTime != null && x.PickupTime >= startOfMonth && x.PickupTime < startOfNextMonth) ||
                (x.PickupTime == null && x.CreatedAt >= startOfMonth && x.CreatedAt < startOfNextMonth)
            );

            var donations = await donationsQuery
                .OrderBy(x => x.PickupTime ?? x.CreatedAt)
                .ToListAsync();

            var summary = new MonthlyDonationSummaryDto
            {
                Year = query.Year,
                Month = query.Month,
                TotalDonationsCount = donations.Count,
                TotalAmount = donations.Sum(x => x.Amount ?? 0)
            };

            var groupedByDay = donations
                .GroupBy(x => (x.PickupTime ?? x.CreatedAt ?? startOfMonth).Day)
                .OrderBy(g => g.Key);

            foreach (var group in groupedByDay)
            {
                var dayDate = new DateTime(query.Year, query.Month, group.Key, 0, 0, 0, DateTimeKind.Utc);
                summary.Days.Add(new DailyDonationSummaryDto
                {
                    Day = group.Key,
                    Date = dayDate.ToString("yyyy-MM-dd"),
                    DonationsCount = group.Count(),
                    Donations = group.Select(MapDonation).ToList()
                });
            }

            return Result<MonthlyDonationSummaryDto>.Success(summary, "Monthly calendar summary retrieved successfully.");
        }

        private async Task<string?> ValidateRecurringRequest(CreateRecurringDonationRequestDto request)
        {
            if (request.StartDate.Year < 2000 || request.StartDate.Year > 2100)
                return "Start date must be between 2000 and 2100.";

            if (request.PickupTimeOfDay < TimeSpan.Zero || request.PickupTimeOfDay >= TimeSpan.FromDays(1)
                || request.DropoffTimeOfDay < TimeSpan.Zero || request.DropoffTimeOfDay >= TimeSpan.FromDays(1))
                return "Pickup and dropoff times must be within a calendar day.";

            if (request.MonasterySpaceId <= 0)
                return "Monastery space id is required.";

            var monasteryExists = await _dbContext.MonasterySpaces.AnyAsync(x => x.Id == request.MonasterySpaceId && !x.IsDeleted);
            if (!monasteryExists)
                return "Monastery not found.";

            if (request.DayOfMonth < 1 || request.DayOfMonth > 31)
                return "Day of month must be between 1 and 31.";

            if (request.TimeZoneOffsetMinutes < -840 || request.TimeZoneOffsetMinutes > 840)
                return "Invalid time zone offset.";

            if (request.Amount < 0 || request.Quantity < 0)
                return "Donation amount and quantity cannot be negative.";

            if (!Enum.IsDefined(request.DonationType))
                return "Invalid donation type.";

            if (request.MonthsCount < 1 || request.MonthsCount > 24)
                return "Months count must be between 1 and 24.";

            if (request.DonationType == DonationType.Other && string.IsNullOrWhiteSpace(request.CustomDonationType))
                return "Custom donation type is required when donation type is Other.";

            return null;
        }

        private async Task<Result<DonationDto>?> ValidateDonationRequest(CreateDonationRequestDto request)
        {
            if (request.MonasterySpaceId <= 0)
            {
                return Result<DonationDto>.Failure("Monastery space id is required.");
            }

            var monasteryExists = await _dbContext.MonasterySpaces.AnyAsync(x => x.Id == request.MonasterySpaceId && !x.IsDeleted);
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

        private Task<bool> IsSystemAdmin(int userId) => _dbContext.Users.AnyAsync(u => u.Id == userId && u.IsDeleted == false && u.Role != null && !u.Role.IsDeleted && u.Role.Name == "Admin");

        private async Task<bool> CanViewDonation(int currentUserId, DonationEntity donation)
        {
            if (donation.DonorId == currentUserId || await IsSystemAdmin(currentUserId))
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
                    && (x.IsOwner == true || _dbContext.SpaceRolePermissions.Any(srp => srp.SpaceRoleId == x.SpaceRoleId && !srp.IsDeleted && !srp.SpaceRole.IsDeleted && !srp.SpacePermission.IsDeleted && srp.SpacePermission.Name == SpacePermissions.DonationReview)))
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
            return member is not null && (member.IsOwner == true || await HasRolePermission(member.SpaceRoleId, SpacePermissions.DonationReview));
        }

        private async Task<bool> CanScheduleDonations(MonasteryMember? member)
        {
            return member is not null && (member.IsOwner == true || await HasRolePermission(member.SpaceRoleId, SpacePermissions.DonationSchedule));
        }

        private Task<bool> HasRolePermission(int spaceRoleId, string permission) => _dbContext.SpaceRolePermissions.AnyAsync(rp =>
            rp.SpaceRoleId == spaceRoleId && !rp.IsDeleted && !rp.SpaceRole.IsDeleted && !rp.SpacePermission.IsDeleted && rp.SpacePermission.Name == permission);

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
