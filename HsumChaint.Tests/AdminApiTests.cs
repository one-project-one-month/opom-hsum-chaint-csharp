using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Shared;
using HsumChaint.Shared.CommonEnum;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HsumChaint.Tests;

public class AdminApiTests
{
    [Fact]
    public async Task SystemReads_RequireActiveAdmin_AndIncludeAllMonasteriesWithoutMembership()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        RbacTestData.Seed(db);
        var admin = new User { Id = 101, Name = "Admin fixture", PhoneNumber = "admin-fixture", RoleId = 1, IsDeleted = false };
        var donor = new User { Id = 102, Name = "Donor fixture", PhoneNumber = "donor-fixture", RoleId = 3, IsDeleted = false };
        foreach (var user in new[] { admin, donor })
            user.Password = new PasswordHasher<User>().HashPassword(user, "Test-password-only");
        db.Users.AddRange(admin, donor);
        db.MonasterySpaces.AddRange(new MonasterySpace { Id = 10, MonasteryName = "First" },
            new MonasterySpace { Id = 20, MonasteryName = "Second" },
            new MonasterySpace { Id = 30, MonasteryName = "Inactive", IsDeleted = true });
        db.DonorLists.AddRange(
            new DonorList { Id = 10, MonasterySpaceId = 10, DonorId = 102, DonorName = "Alpha", StatusValue = DonationStatus.PendingReview, CreatedAt = new DateTime(2026, 10, 1) },
            new DonorList { Id = 20, MonasterySpaceId = 20, DonorId = 102, DonorName = "Beta", StatusValue = DonationStatus.Completed, CreatedAt = new DateTime(2026, 10, 2) });
        await db.SaveChangesAsync();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        foreach (var path in new[] { "summary", "donations" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/admin/{path}")).StatusCode);

        async Task Login(string phone)
        {
            var response = await client.PostAsJsonAsync("/api/v1/Auth/login", new { PhoneNumber = phone, Password = "Test-password-only" });
            var login = await response.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
            Assert.True(login!.IsSuccess, login.Message);
            client.DefaultRequestHeaders.Authorization = new("Bearer", login.Data!.AccessToken);
        }
        await Login("donor-fixture");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/donations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/summary")).StatusCode);
        await Login("admin-fixture");
        using var summary = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/summary"));
        Assert.Equal(2, summary.RootElement.GetProperty("data").GetProperty("donations").GetInt32());
        Assert.Equal(2, summary.RootElement.GetProperty("data").GetProperty("monasteries").GetInt32());
        Assert.Equal(1, summary.RootElement.GetProperty("data").GetProperty("pendingReview").GetInt32());
        Assert.Equal(1, summary.RootElement.GetProperty("data").GetProperty("completed").GetInt32());
        using var page = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/donations?PageSize=1"));
        Assert.Equal(2, page.RootElement.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        Assert.Equal("Second", page.RootElement.GetProperty("data")[0].GetProperty("monasteryName").GetString());
        using var filtered = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/donations?Status=PendingReview&Search=Alpha"));
        Assert.Equal(10, filtered.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32());
        using var dateFiltered = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/donations?FromDate=2026-10-02T00:00:00Z"));
        Assert.Equal(1, dateFiltered.RootElement.GetProperty("data").GetArrayLength());
        foreach (var query in new[] { "PageNumber=0", "PageSize=101", "Status=99", "FromDate=2026-10-03&ToDate=2026-10-01" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/admin/donations?{query}")).StatusCode);

        // Exercise portal writes through the real HTTP pipeline, without monastery membership.
        async Task<JsonElement> Write(HttpMethod method, string path, object? payload = null, HttpStatusCode expected = HttpStatusCode.OK)
        {
            using var request = new HttpRequestMessage(method, path);
            if (payload != null) request.Content = JsonContent.Create(payload);
            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"{method} {path}: expected {expected}, got {response.StatusCode}: {content}");
            using var document = JsonDocument.Parse(content);
            return document.RootElement.Clone();
        }
        var recorded = await Write(HttpMethod.Post, "/api/v1/donations/manual", new { MonasterySpaceId = 10, DonorName = "Walk-in", DonationType = 1, Quantity = 12 });
        var donationId = recorded.GetProperty("data").GetProperty("id").GetInt32();
        await Write(HttpMethod.Post, $"/api/v1/donations/{donationId}/review", new { Status = 3, Note = "Reviewed by system Admin" });
        await Write(HttpMethod.Put, $"/api/v1/donations/{donationId}/schedule", new { PickupTime = "2026-11-01T02:30:00Z" });
        await Write(HttpMethod.Post, $"/api/v1/donations/{donationId}/complete");
        await Write(HttpMethod.Post, $"/api/v1/donations/{donationId}/cancel", expected: HttpStatusCode.BadRequest);
        await Write(HttpMethod.Delete, $"/api/v1/donations/{donationId}", expected: HttpStatusCode.BadRequest);
        var batch = await Write(HttpMethod.Post, "/api/v1/donations/batch", new { MonasterySpaceId = 20, Donations = new[] { new { DonorName = "Batch A", DonationType = 1, Quantity = 1 }, new { DonorName = "Batch B", DonationType = 1, Quantity = 2 } } });
        Assert.Equal(2, batch.GetProperty("data").GetArrayLength());
        var deleteId = batch.GetProperty("data")[0].GetProperty("id").GetInt32();
        await Write(HttpMethod.Post, $"/api/v1/donations/{deleteId}/cancel");
        await Write(HttpMethod.Delete, $"/api/v1/donations/{deleteId}");
        var recurring = await Write(HttpMethod.Post, "/api/v1/donations/recurring/manual", new { MonasterySpaceId = 20, DonorName = "Monthly", DonationType = 1, Quantity = 1, StartDate = "2027-02-01", DayOfMonth = 31, MonthsCount = 2, PickupTimeOfDay = "03:00:00", TimeZoneOffsetMinutes = 390 });
        Assert.Equal(2, recurring.GetProperty("data").GetArrayLength());
        Assert.StartsWith("2027-02-27T20:30:00", recurring.GetProperty("data")[0].GetProperty("pickupTime").GetString());
        using var calendar = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/donations?CalendarMonth=2027-02"));
        Assert.Single(calendar.RootElement.GetProperty("data").EnumerateArray());
        await Write(HttpMethod.Patch, "/api/v1/monasteries/10", new { MonasteryName = "Admin edited", Address = "Yangon" });
        await Write(HttpMethod.Delete, "/api/v1/monasteries/10", expected: HttpStatusCode.Forbidden);
        var own = await Write(HttpMethod.Post, "/api/v1/monasteries", new { MonasteryName = "Admin owned", Address = "Yangon" });
        await Write(HttpMethod.Delete, $"/api/v1/monasteries/{own.GetProperty("data").GetProperty("id").GetInt32()}");
        var invitation = await Write(HttpMethod.Post, "/api/v1/monasteries/10/invitations", new { UserId = 102, SpaceRoleId = 4 });
        var invitationId = invitation.GetProperty("data").GetProperty("invitationId").GetInt32();
        await Write(HttpMethod.Post, "/api/v1/monasteries/10/invitations", new { UserId = 102, SpaceRoleId = 4 }, HttpStatusCode.BadRequest);
        await Write(HttpMethod.Post, $"/api/v1/monasteries/invitations/{invitationId}/respond", new { Status = 1 }, HttpStatusCode.Forbidden);
        await Login("donor-fixture");
        await Write(HttpMethod.Post, $"/api/v1/monasteries/invitations/{invitationId}/respond", new { Status = 1 });
        await Login("admin-fixture");
        await Write(HttpMethod.Put, "/api/v1/monasteries/10/members/102/role", new { SpaceRoleId = 3 });
        await Write(HttpMethod.Delete, "/api/v1/monasteries/10/members/102");
        var nextInvite = await Write(HttpMethod.Post, "/api/v1/monasteries/10/invitations", new { UserId = 102, SpaceRoleId = 4 });
        await Write(HttpMethod.Delete, $"/api/v1/admin/invitations/{nextInvite.GetProperty("data").GetProperty("invitationId").GetInt32()}");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/invitations")).StatusCode);
        await Write(HttpMethod.Post, "/api/v1/admin/notifications", new { UserId = 102, Message = "Admin announcement", Type = 2 });
        using var notifications = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/notifications"));
        var notificationId = notifications.RootElement.GetProperty("data").EnumerateArray().First(n => n.GetProperty("message").GetString() == "Admin announcement").GetProperty("id").GetInt32();
        await Write(HttpMethod.Patch, $"/api/v1/admin/notifications/{notificationId}/read");
        await Write(HttpMethod.Post, "/api/v1/admin/notifications/read-all");
        await Write(HttpMethod.Delete, $"/api/v1/admin/notifications/{notificationId}");
        await Write(HttpMethod.Delete, "/api/v1/admin/notifications");
        using var cleared = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/notifications"));
        Assert.Equal(0, cleared.RootElement.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        await Write(HttpMethod.Post, "/api/v1/admin/notifications", new { UserId = 0, Message = "", Type = 99 }, HttpStatusCode.BadRequest);
        await Write(HttpMethod.Delete, "/api/v1/admin/users/101", expected: HttpStatusCode.BadRequest);
        await Write(HttpMethod.Put, "/api/v1/admin/users/101", new { Name = "Admin", PhoneNumber = "admin-fixture", RoleId = 3 }, HttpStatusCode.BadRequest);
        await Write(HttpMethod.Post, "/api/v1/admin/users", new { Name = "New user", PhoneNumber = "new-fixture", Password = "test-password", RoleId = 3 });
        var createdUser = await db.Users.AsNoTracking().SingleAsync(u => u.PhoneNumber == "new-fixture");
        await Write(HttpMethod.Put, $"/api/v1/admin/users/{createdUser.Id}", new { Name = "Edited user", PhoneNumber = "new-fixture", RoleId = 4 });
        await Write(HttpMethod.Put, $"/api/v1/admin/users/{createdUser.Id}", new { Name = "Edited user", PhoneNumber = "admin-fixture", RoleId = 4 }, HttpStatusCode.BadRequest);
        await Write(HttpMethod.Delete, $"/api/v1/admin/users/{createdUser.Id}");
        admin.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/summary")).StatusCode);
        admin.IsDeleted = false;
        admin.RoleId = 3;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/donations")).StatusCode);
    }
}
