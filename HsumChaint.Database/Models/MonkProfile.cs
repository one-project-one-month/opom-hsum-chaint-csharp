using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class MonkProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? MonasteryName { get; set; }

    public string? MonasteryAddress { get; set; }
}
