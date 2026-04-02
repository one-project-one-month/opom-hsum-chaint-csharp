using HsumChaint.Infrastructure.Models;
using HsumChaint.Infrastructure.RepositoryInterfaces;

namespace HsumChaint.Infrastructure.Repositories
{
    public class NotificationRepository(AppDbContext _dbContext) : INotificationRepository
    {
        #region Create Notification
        public async Task<CommonResponseModel<Notification>> Create(Notification requestModel)
        {
            var response = new CommonResponseModel<Notification>();
            try
            {
                await _dbContext.Notifications.AddAsync(requestModel);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Notification added successfully.";
                response.Data = requestModel;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }
        #endregion

        #region Get Notification By Id
        public async Task<CommonResponseModel<Notification>> GetById(int id)
        {
            var response = new CommonResponseModel<Notification>();
            try
            {
                var notification = await _dbContext.Notifications.FindAsync(id);

                if (notification == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Notification not found.";
                    return response;
                }

                response.IsSuccess = true;
                response.Data = notification;
                response.Message = "Notification retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }
        #endregion

        #region Update Notification
        public async Task<CommonResponseModel<Notification>> Update(Notification requestModel)
        {
            var response = new CommonResponseModel<Notification>();
            try
            {
                _dbContext.Notifications.Update(requestModel);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Data = requestModel;
                response.Message = "Notification updated successfully.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }
        #endregion
    }
}
