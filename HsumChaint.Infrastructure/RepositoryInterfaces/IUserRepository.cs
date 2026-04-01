using HsumChaint.Infrastructure.Models;

namespace HsumChaint.Infrastructure.RepositoryInterfaces
{
    public interface IUserRepository
    {
        Task<CommonResponseModel<User>> AddUser(User user);

        Task<CommonResponseModel<List<User>>> GetAllUsers();

        Task<CommonResponseModel<User>> GetUser(int id);

        Task<CommonResponseModel<User>> PutUser(User user);
    }
}