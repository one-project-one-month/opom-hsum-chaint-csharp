using System;
using System.Collections.Generic;

namespace HsumChaint.Infrastructure.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public string? MonasteryName { get; set; }

    public string? MonasteryAddress { get; set; }

    public string? Email { get; set; }

    public string? ContactPhoneNumber { get; set; }

    public string? FcmToken { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }
}
