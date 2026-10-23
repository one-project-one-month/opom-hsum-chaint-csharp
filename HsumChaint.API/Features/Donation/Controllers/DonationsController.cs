using HsumChaint.API.Authorization;
using HsumChaint.Shared.Authorization;
using HsumChaint.API.Extensions;
using HsumChaint.Domain.Features.Donation.DTOs;
using HsumChaint.Domain.Features.Donation.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Features.Donation.Controllers
{
    [Authorize]
    [Route("api/v1/donations")]
    [ApiController]
    public class DonationsController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public DonationsController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        [HttpPost("request")]
        [HasPermission(Permissions.Donation.Create)]
        public async Task<IActionResult> RequestDonation(CreateDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.RequestDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("manual")]
        [HasPermission(Permissions.Donation.Create)]
        public async Task<IActionResult> CreateManualDonation(CreateManualDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.CreateManualDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpGet]
        [HasPermission(Permissions.Donation.View)]
        public async Task<IActionResult> GetDonations([FromQuery] DonationQueryDto query)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.GetDonations(currentUserId.Value, query);
            return response.ToActionResult();
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.Donation.View)]
        public async Task<IActionResult> GetDonation(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new GetDonationRequestDto { Id = id };
            var response = await _donationService.GetDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("{id}/review")]
        [HasPermission(Permissions.Donation.Review)]
        public async Task<IActionResult> ReviewDonation(int id, ReviewDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            request.Id = id;
            var response = await _donationService.ReviewDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPut("{id}/schedule")]
        [HasPermission(Permissions.Donation.Schedule)]
        public async Task<IActionResult> ScheduleDonation(int id, ScheduleDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            request.Id = id;
            var response = await _donationService.ScheduleDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("{id}/complete")]
        [HasPermission(Permissions.Donation.Schedule)]
        public async Task<IActionResult> CompleteDonation(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new CompleteDonationRequestDto { Id = id };
            var response = await _donationService.CompleteDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("{id}/cancel")]
        [HasPermission(Permissions.Donation.Cancel)]
        public async Task<IActionResult> CancelDonation(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new CancelDonationRequestDto { Id = id };
            var response = await _donationService.CancelDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("batch")]
        [HasPermission(Permissions.Donation.Create)]
        public async Task<IActionResult> CreateBatchDonations(CreateBatchDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.CreateBatchManualDonations(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("recurring/request")]
        [HasPermission(Permissions.Donation.Create)]
        public async Task<IActionResult> RequestRecurringDonation(CreateRecurringDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.RequestRecurringDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("recurring/manual")]
        [HasPermission(Permissions.Donation.Create)]
        public async Task<IActionResult> CreateManualRecurringDonation(CreateManualRecurringDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.CreateManualRecurringDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Donation.Delete)]
        public async Task<IActionResult> DeleteDonation(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new DeleteDonationRequestDto { Id = id };
            var response = await _donationService.DeleteDonation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpGet("calendar")]
        [HasPermission(Permissions.Donation.View)]
        public async Task<IActionResult> GetMonthlyCalendar([FromQuery] MonthlyCalendarQueryDto query)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.GetMonthlyCalendar(currentUserId.Value, query);
            return response.ToActionResult();
        }
    }
}
