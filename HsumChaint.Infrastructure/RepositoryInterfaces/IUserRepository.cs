using HsumChaint.Infrastructure.Models;

namespace HsumChaint.Infrastructure.RepositoryInterfaces
{
    public interface IUserRepository
    {
        Task<CommonResponseModel<List<User>>> GetAllUsers();

        Task<CommonResponseModel<User>> GetUser(int id);

        Task<CommonResponseModel<User>> PutUser(User user);

        Task<CommonResponseModel<User>> DeleteUser(int id);
    }
}