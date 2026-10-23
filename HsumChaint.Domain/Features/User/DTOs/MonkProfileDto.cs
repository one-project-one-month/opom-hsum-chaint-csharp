namespace HsumChaint.Domain.Features.User.DTOs;

public class MonkProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? MonasteryName { get; set; }
    public string? MonasteryAddress { get; set; }
}

public class GetMonkProfileRequestDto
{
    public int UserId { get; set; }
}

public class UpdateMonkProfileRequestDto
{
    public int UserId { get; set; }
    public string? MonasteryName { get; set; }
    public string? MonasteryAddress { get; set; }
}

public class DeleteMonkProfileRequestDto
{
    public int UserId { get; set; }
}
