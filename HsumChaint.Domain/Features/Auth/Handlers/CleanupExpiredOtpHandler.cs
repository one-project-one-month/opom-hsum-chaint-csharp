using System;
using System.Threading;
using System.Threading.Tasks;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HsumChaint.Domain.Features.Auth.Handlers;

public sealed class CleanupExpiredOtpHandler : INotificationHandler<CleanupExpiredOtpEvent>
{
    private readonly AppDbContext _context;
    private readonly ILogger<CleanupExpiredOtpHandler> _logger;

    public CleanupExpiredOtpHandler(
        AppDbContext context,
        ILogger<CleanupExpiredOtpHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(CleanupExpiredOtpEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            await _context.OtpCodes
                .Where(x => x.IsUsed || x.ExpiresAt <= now || x.RevokedAt != null)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up expired OTP records.");
        }
    }
}
