using HsumChaint.Domain.Features.Notification.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Notification.ServiceInterfaces
{
    public interface INotificationService
    {
        Task<ApplicationCommonResponseModel<CreateNotificationResponseDto>> SendNotificationAndStore(CreateNotificationRequestDto requestModel);
        Task<ApplicationCommonResponseModel<ReadNotificationResponseDto>> ReadNotification(ReadNotificationRequestDto requestModel);
        Task<ApplicationCommonResponseModel<DeleteNotificationResponseDto>> DeleteNotification(DeleteNotificationRequestDto requestModel);
    }
}






