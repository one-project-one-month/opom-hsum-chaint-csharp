using MediatR;

namespace HsumChaint.Domain.Features.Auth.Events;

public sealed record CleanupExpiredOtpEvent : INotification;
