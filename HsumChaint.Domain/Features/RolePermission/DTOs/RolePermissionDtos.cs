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

public class GetRoleRequestDto
{
    public int Id { get; set; }
}

public class DeleteRoleRequestDto
{
    public int Id { get; set; }
}

public class UpdateRoleRequestDto : CreateRoleRequestDto
{
    public int Id { get; set; }
}

public class AssignRolePermissionsRequestDto
{
    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }
    [Required]
    public List<int> PermissionIds { get; set; } = new();
}

public class SpacePermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SpaceRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<SpacePermissionDto> Permissions { get; set; } = new();
}

public class CreateSpaceRoleRequestDto
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
    [StringLength(255)]
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}

public class GetSpaceRoleRequestDto
{
    public int Id { get; set; }
}

public class DeleteSpaceRoleRequestDto
{
    public int Id { get; set; }
}

public class UpdateSpaceRoleRequestDto
{
    public int Id { get; set; }
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
    [StringLength(255)]
    public string? Description { get; set; }
    public List<int>? PermissionIds { get; set; }
}

public class AssignSpaceRolePermissionsRequestDto
{
    [Range(1, int.MaxValue)]
    public int SpaceRoleId { get; set; }
    [Required]
    public List<int> PermissionIds { get; set; } = new();
}
