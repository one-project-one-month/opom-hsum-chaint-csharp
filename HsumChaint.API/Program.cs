using HsumChaint.API.Extensions;
using HsumChaint.Domain.Extensions;
using HsumChaint.Database.Extensions;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
const string localFlutterCorsPolicy = "LocalFlutterCors";

builder.AddStageConfig();
builder.AddFirebaseConfiguration();

var configuredCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(origin => origin.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!)
    .ToArray();

var corsOrigins = configuredCorsOrigins.Length > 0
    ? configuredCorsOrigins
    : builder.Environment.IsDevelopment()
        ? new[]
        {
            "http://localhost:3000",
            "http://localhost:5000",
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:8080",
            "http://localhost:8081",
            "http://127.0.0.1:3000",
            "http://127.0.0.1:5000",
            "http://127.0.0.1:5173",
            "http://127.0.0.1:5174",
            "http://127.0.0.1:8080",
            "http://127.0.0.1:8081"
        }
        : Array.Empty<string>();

builder.Services
    .AddApiServices()
    .AddJwtAuthentication(builder.Configuration)
    .AddDomainServices()
    .AddDatabaseServices(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hsum Chaint API",
        Version = "v1",
        Description = "Hsum Chaint Backend API"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(localFlutterCorsPolicy, policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapSwagger("/openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Hsum Chaint API")
            .WithTheme(ScalarTheme.Moon)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseCors(localFlutterCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
