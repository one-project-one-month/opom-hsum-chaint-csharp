using System.Text.Json;
using HsumChaint.API.Extensions;
using HsumChaint.Shared;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HsumChaint.Tests;

public class ResultAndPaginationTests
{
    [Fact]
    public void Results_PreserveSuccessFailureAndData()
    {
        var success = Result.Success();
        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.Equal("Success", success.Message);
        var failure = Result.Failure("Invalid request");
        Assert.True(failure.IsFailure);
        Assert.Equal("Invalid request", failure.Message);
        Assert.Equal(42, Result<int>.Success(42).Data);
        Assert.Null(Result<string>.Failure("Missing").Data);
    }

    [Theory]
    [InlineData(1, 10, 0, 0, false, false)]
    [InlineData(1, 10, 10, 1, false, false)]
    [InlineData(1, 10, 11, 2, false, true)]
    [InlineData(2, 10, 11, 2, true, false)]
    [InlineData(3, 10, 11, 2, true, false)]
    [InlineData(0, 0, 0, 0, false, false)]
    [InlineData(1, 1, int.MaxValue, int.MaxValue, false, true)]
    public void Pagination_ComputesPageBoundaries(int page, int size, int count, int pages, bool previous, bool next)
    {
        var pagination = new Pagination(page, size, count);
        Assert.Equal(pages, pagination.TotalPages);
        Assert.Equal(previous, pagination.HasPreviousPage);
        Assert.Equal(next, pagination.HasNextPage);
    }

    [Fact]
    public void PagedResults_ProvideDataAndEmptyFailureMetadata()
    {
        var success = PagedResult<int>.Success(new() { 11, 12 }, new Pagination(2, 10, 12), "Retrieved");
        Assert.True(success.IsSuccess);
        Assert.Equal(new[] { 11, 12 }, success.Data);
        Assert.Equal(12, success.Pagination.TotalCount);
        var failure = PagedResult<int>.Failure("Invalid page");
        Assert.True(failure.IsFailure);
        Assert.Empty(failure.Data);
        Assert.Equal(0, failure.Pagination.TotalPages);
        Assert.Equal(0, failure.Pagination.PageSize);
        var request = new PaginationRequest();
        Assert.Equal(1, request.PageNumber);
        Assert.Equal(10, request.PageSize);
    }

    [Fact]
    public void Models_RoundTripWithWebJsonAndUseDataInsteadOfListData()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var result = Result<string>.Success("value", "Retrieved");
        var resultCopy = JsonSerializer.Deserialize<Result<string>>(JsonSerializer.Serialize(result, options), options)!;
        Assert.Equal(result.Data, resultCopy.Data);
        Assert.Equal(result.Message, resultCopy.Message);
        var page = PagedResult<int>.Success(new() { 11, 12 }, new Pagination(2, 10, 12));
        var json = JsonSerializer.Serialize(page, options);
        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("data", out _));
        Assert.False(document.RootElement.TryGetProperty("listData", out _));
        var copy = JsonSerializer.Deserialize<PagedResult<int>>(json, options)!;
        Assert.Equal(page.Data, copy.Data);
        Assert.Equal(2, copy.Pagination.PageNumber);
        Assert.Equal(2, copy.Pagination.TotalPages);
        Assert.True(copy.Pagination.HasPreviousPage);
        Assert.False(copy.Pagination.HasNextPage);
        Assert.True(JsonSerializer.Deserialize<Result>(JsonSerializer.Serialize(Result.Success(), options), options)!.IsSuccess);
    }

    [Theory]
    [InlineData(true, "Success", 200)]
    [InlineData(false, "User NOT FOUND", 404)]
    [InlineData(false, "User is not authorized", 403)]
    [InlineData(false, "User is not a member", 403)]
    [InlineData(false, "Unauthorized access to this notification.", 403)]
    [InlineData(false, "Invalid page number", 400)]
    public void ActionResults_MapStatusAndPreserveResponse(bool success, string message, int status)
    {
        var response = new Result(success, message);
        var action = Assert.IsAssignableFrom<ObjectResult>(response.ToActionResult());
        Assert.Equal(status, action.StatusCode);
        Assert.Same(response, action.Value);
    }
}
