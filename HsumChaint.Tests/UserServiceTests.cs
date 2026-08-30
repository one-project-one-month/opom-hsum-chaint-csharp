using AutoMapper;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.User.DTOs;
using HsumChaint.Domain.Features.User.Services;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using InfrastructureUser = HsumChaint.Database.Models.User;

namespace HsumChaint.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetAllUsers_ReturnsMappedUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            new InfrastructureUser { Id = 1, Name = "Alice", PhoneNumber = "1111111111", Password = "pw", UserType = UserType.User, IsDeleted = false },
            new InfrastructureUser { Id = 2, Name = "Bob", PhoneNumber = "2222222222", Password = "pw", UserType = UserType.User, IsDeleted = false });
        await dbContext.SaveChangesAsync();

        var userService = new UserService(dbContext, CreateMapper().Object);

        var response = await userService.GetAllUsers();

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.ListData);
        Assert.Equal(2, response.ListData!.Count);
        Assert.Equal("Alice", response.ListData.First().Name);
    }

    [Fact]
    public async Task PutUser_ReturnsSuccess_WhenUpdateSucceeds()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new InfrastructureUser
        {
            Id = 1,
            Name = "Original",
            PhoneNumber = "1111111111",
            Password = "pw",
            UserType = UserType.User,
            IsDeleted = false
        });
        await dbContext.SaveChangesAsync();

        var userService = new UserService(dbContext, CreateMapper().Object);

        var response = await userService.PutUser(new UserDto
        {
            Id = 1,
            Name = "Updated",
            PhoneNumber = "3333333333",
            UserType = UserType.User
        });

        Assert.True(response.IsSuccess);
        Assert.Null(response.Data);
        Assert.Equal("Updated", (await dbContext.Users.FindAsync(1))!.Name);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Mock<IMapper> CreateMapper()
    {
        var mapper = new Mock<IMapper>();

        mapper
            .Setup(x => x.Map<List<UserDto>>(It.IsAny<List<InfrastructureUser>>()))
            .Returns((List<InfrastructureUser> users) => users.Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType
            }).ToList());

        mapper
            .Setup(x => x.Map<UserDto>(It.IsAny<InfrastructureUser?>()))
            .Returns((InfrastructureUser? user) => user == null ? null! : new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType
            });

        mapper
            .Setup(x => x.Map<InfrastructureUser>(It.IsAny<UserDto>()))
            .Returns((UserDto dto) => new InfrastructureUser
            {
                Id = dto.Id,
                Name = dto.Name!,
                PhoneNumber = dto.PhoneNumber!,
                UserType = dto.UserType,
                Email = dto.Email,
                ContactPhoneNumber = dto.ContactPhoneNumber
            });

        return mapper;
    }
}
