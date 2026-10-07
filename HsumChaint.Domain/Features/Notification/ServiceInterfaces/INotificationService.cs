using HsumChaint.Shared;
using HsumChaint.Domain.Features.Notification.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Notification.ServiceInterfaces
{
    public interface INotificationService
    {
        Task<Result<CreateNotificationResponseDto>> SendNotificationAndStore(CreateNotificationRequestDto requestModel);
        Task<Result<ReadNotificationResponseDto>> ReadNotification(ReadNotificationRequestDto requestModel);
        Task<Result<DeleteNotificationResponseDto>> DeleteNotification(DeleteNotificationRequestDto requestModel);
    }
}






