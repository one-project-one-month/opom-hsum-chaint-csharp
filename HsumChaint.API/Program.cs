using HsumChaint.API.Extensions;
using HsumChaint.Domain.Extensions;
using HsumChaint.Database.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddStageConfig();
builder.AddFirebaseConfiguration();

builder.Services
    .AddApiServices()
    .AddJwtAuthentication(builder.Configuration)
    .AddDomainServices()
    .AddDatabaseServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
