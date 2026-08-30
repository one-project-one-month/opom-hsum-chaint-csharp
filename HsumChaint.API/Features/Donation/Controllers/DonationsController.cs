using HsumChaint.API.Extensions;
using HsumChaint.Domain;
using HsumChaint.Domain.Features.Donation.DTOs;
using HsumChaint.Domain.Features.Donation.ServiceInterfaces;
using HsumChaint.Shared.CommonEnum;
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
        public async Task<IActionResult> RequestDonation(CreateDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.RequestDonation(currentUserId.Value, request);
            return ToActionResult(response);
        }

        [HttpPost("manual")]
        public async Task<IActionResult> CreateManualDonation(CreateManualDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.CreateManualDonation(currentUserId.Value, request);
            return ToActionResult(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetDonations(
            [FromQuery] int? monasterySpaceId,
            [FromQuery] int? donorId,
            [FromQuery] DonationStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.GetDonations(currentUserId.Value, new DonationQueryDto
            {
                MonasterySpaceId = monasterySpaceId,
                DonorId = donorId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            });
            return ToActionResult(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDonation(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.GetDonation(currentUserId.Value, id);
            return ToActionResult(response);
        }

        [HttpPut("{id}/review")]
        public async Task<IActionResult> ReviewDonation(int id, ReviewDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.ReviewDonation(currentUserId.Value, id, request);
            return ToActionResult(response);
        }

        [HttpPut("{id}/schedule")]
        public async Task<IActionResult> ScheduleDonation(int id, ScheduleDonationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.ScheduleDonation(currentUserId.Value, id, request);
            return ToActionResult(response);
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteDonation(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.CompleteDonation(currentUserId.Value, id);
            return ToActionResult(response);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelDonation(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _donationService.CancelDonation(currentUserId.Value, id);
            return ToActionResult(response);
        }

        private IActionResult ToActionResult<T>(ApplicationCommonResponseModel<T> response) where T : class
        {
            if (response.IsSuccess == true)
            {
                return Ok(response);
            }

            if (response.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(response);
            }

            if (response.Message?.Contains("not authorized", StringComparison.OrdinalIgnoreCase) == true
                || response.Message?.Contains("not a member", StringComparison.OrdinalIgnoreCase) == true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            return BadRequest(response);
        }
    }
}
