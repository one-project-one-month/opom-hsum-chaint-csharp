using HsumChaint.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.ServiceInterfaces
{
    public interface INotificationService
    {
        Task<bool> SendNotificationAndStore(CreateNotificationRequestDto requestModel);
    }
}
