using HsumChaint.Shared;
using HsumChaint.API.Authorization;
using HsumChaint.Shared.Authorization;
using HsumChaint.API.Extensions;
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetMonasteries([FromQuery] GetMonasteriesRequestDto request)
        {
            var response = await _monasteryService.GetMonasteries(request ?? new GetMonasteriesRequestDto());
            return response.ToActionResult();
        }

        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> SearchMonasteries([FromQuery] SearchMonasteriesRequestDto request)
        {
            var response = await _monasteryService.SearchMonasteries(request ?? new SearchMonasteriesRequestDto());
            return response.ToActionResult();
        }

        [HttpPost]
        [HasPermission(Permissions.Monastery.Create)]
        public async Task<IActionResult> Create(CreateMonasteryRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.CreateMonastery(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine([FromQuery] PaginationRequest pagination)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.GetMyMonasteries(currentUserId.Value, pagination);
            return response.ToActionResult();
        }

        [HttpGet("{id}")]
        [HasPermission(SpacePermissions.SpaceView)]
        public async Task<IActionResult> Get(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new GetMonasteryRequestDto { Id = id };
            var response = await _monasteryService.GetMonastery(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPatch("{id}")]
        [HasPermission(SpacePermissions.SpaceUpdate)]
        public async Task<IActionResult> Update(int id, UpdateMonasteryRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            request.Id = id;
            var response = await _monasteryService.UpdateMonastery(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpDelete("{id}")]
        [HasPermission(SpacePermissions.SpaceDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new DeleteMonasteryRequestDto { Id = id };
            var response = await _monasteryService.DeleteMonastery(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("{id}/invitations")]
        [HasPermission(SpacePermissions.SpaceInviteMember)]
        public async Task<IActionResult> InviteMember(int id, InviteMemberRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            request.MonasterySpaceId = id;
            var response = await _monasteryService.InviteMember(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPost("invitations/{invitationId}/respond")]
        [HasPermission(Permissions.Invitation.Respond)]
        public async Task<IActionResult> RespondToInvitation(int invitationId, RespondInvitationRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            request.InvitationId = invitationId;
            var response = await _monasteryService.RespondToInvitation(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpGet("{id}/members")]
        [HasPermission(SpacePermissions.SpaceViewMembers)]
        public async Task<IActionResult> GetMembers(int id, [FromQuery] PaginationRequest pagination)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new GetMonasteryMembersRequestDto
            {
                MonasterySpaceId = id,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
            var response = await _monasteryService.GetMembers(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpPut("{id}/members/{memberUserId}/role")]
        [HasPermission(SpacePermissions.SpaceUpdateMemberRole)]
        public async Task<IActionResult> UpdateMemberRole(int id, int memberUserId, UpdateMemberRoleRequestDto request)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            request.MonasterySpaceId = id;
            request.MemberUserId = memberUserId;
            var response = await _monasteryService.UpdateMemberRole(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpDelete("{id}/members/{memberUserId}")]
        [HasPermission(SpacePermissions.SpaceRemoveMember)]
        public async Task<IActionResult> RemoveMember(int id, int memberUserId)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var request = new RemoveMemberRequestDto
            {
                MonasterySpaceId = id,
                MemberUserId = memberUserId
            };
            var response = await _monasteryService.RemoveMember(currentUserId.Value, request);
            return response.ToActionResult();
        }

        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetPermissions(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.GetSpacePermissions(currentUserId.Value, id);
            return response.ToActionResult();
        }

        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetRoles(int id)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var response = await _monasteryService.GetSpaceRoles(currentUserId.Value, id);
            return response.ToActionResult();
        }
    }
}
