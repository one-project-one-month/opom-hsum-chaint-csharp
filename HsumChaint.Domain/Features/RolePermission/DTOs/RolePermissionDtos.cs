using System.ComponentModel.DataAnnotations;

namespace HsumChaint.Domain.Features.RolePermission.DTOs;

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class CreateRoleRequestDto
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public List<int> PermissionIds { get; set; } = new();
}

public class UpdateRoleRequestDto : CreateRoleRequestDto { }

public class AssignRolePermissionsRequestDto
{
    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }
    [Required]
    public List<int> PermissionIds { get; set; } = new();
}
