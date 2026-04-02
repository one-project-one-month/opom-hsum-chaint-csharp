using HsumChaint.Application.DTOs.Notification;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.ServiceInterfaces
{
    public interface INotificationService
    {
        Task<ApplicationCommonResponseModel<CreateNotificationResponseDto>> SendNotificationAndStore(CreateNotificationRequestDto requestModel);
        Task<ApplicationCommonResponseModel<ReadNotificationResponseDto>> ReadNotification(ReadNotificationRequestDto requestModel);
        Task<ApplicationCommonResponseModel<DeleteNotificationResponseDto>> DeleteNotification(DeleteNotificationRequestDto requestModel);
    }
}
