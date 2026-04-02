using HsumChaint.Infrastructure.Models;

namespace HsumChaint.Infrastructure.RepositoryInterfaces
{
    public interface INotificationRepository
    {
        Task<CommonResponseModel<Notification>> Create(Notification requestModel);
        Task<CommonResponseModel<Notification>> GetById(int id);
        Task<CommonResponseModel<Notification>> Update(Notification requestModel);
    }
}
