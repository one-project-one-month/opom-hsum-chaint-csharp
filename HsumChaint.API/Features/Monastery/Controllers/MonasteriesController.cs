using HsumChaint.API.Extensions;
using HsumChaint.Domain;
using HsumChaint.Domain.Features.Monastery.DTOs;
using HsumChaint.Domain.Features.Monastery.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Features.Monastery.Controllers
{
    [Authorize]
    [Route("api/v1/monasteries")]
    [ApiController]
    public class MonasteriesController : ControllerBase
    {
        private readonly IMonasteryService _monasteryService;

        public MonasteriesController(IMonasteryService monasteryService)
        {
            _monasteryService = monasteryService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMonasteryRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.CreateMonastery(currentUserId.Value, request);
            return ToActionResult(response);
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.GetMyMonasteries(currentUserId.Value);
            return ToActionResult(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.GetMonastery(currentUserId.Value, id);
            return ToActionResult(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateMonasteryRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.UpdateMonastery(currentUserId.Value, id, request);
            return ToActionResult(response);
        }

        [HttpPost("{id}/invitations")]
        public async Task<IActionResult> InviteMember(int id, InviteMemberRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.InviteMember(currentUserId.Value, id, request);
            return ToActionResult(response);
        }

        [HttpPost("invitations/{invitationId}/respond")]
        public async Task<IActionResult> RespondToInvitation(int invitationId, RespondInvitationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.RespondToInvitation(currentUserId.Value, invitationId, request);
            return ToActionResult(response);
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.GetMembers(currentUserId.Value, id);
            return ToActionResult(response);
        }

        [HttpPut("{id}/members/{memberUserId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int id, int memberUserId, UpdateMemberRoleRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.UpdateMemberRole(currentUserId.Value, id, memberUserId, request);
            return ToActionResult(response);
        }

        [HttpDelete("{id}/members/{memberUserId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberUserId)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.RemoveMember(currentUserId.Value, id, memberUserId);
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
