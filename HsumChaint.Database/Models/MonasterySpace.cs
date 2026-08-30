using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class MonasterySpace
{
    public int Id { get; set; }

    public string? MonasteryName { get; set; }

    public string? Description { get; set; }

    public string? Address { get; set; }

    public int? CreatedById { get; set; }
}
