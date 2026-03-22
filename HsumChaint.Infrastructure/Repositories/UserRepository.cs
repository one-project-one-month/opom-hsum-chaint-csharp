using HsumChaint.Infrastructure.Models;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        #region AddUser
        public async Task<CommonResponseModel<User>> AddUser(User user)
        {
            var response = new CommonResponseModel<User>();

            try
            {
                await _context.Users.AddAsync(user);

                var saveResponse = await _context.SaveChangesAsync();

                if (saveResponse > 0)
                {
                    response.IsSuccess = true;
                    response.Message = "add user successfully";
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = "something went wrong";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Repo Layer  Exception :{ex.Message}";
            }
            return response;
        }

        public async Task<CommonResponseModel<List<User>>> GetAllUsers()
        {
            var response = new CommonResponseModel<List<User>>();

            try
            {
                List<User> userList = _context.Users.Where(user => user.IsDeleted == false).ToList();

                if (userList.Count() > 0)
                {
                    response.ListData = userList;
                    response.IsSuccess = true;
                    response.Message = "Successfully Retrieved User Lists";
                }
                else
                {
                    response.ListData = userList;
                    response.IsSuccess = true;
                    response.Message = "User list not found";
                }
            }
            catch (Exception ex)
            {
                // T.B.D : Should I add null value List Data
                response.IsSuccess = false;
                response.Message = $"Repo Layer  Exception :{ex.Message}";
            }
            return response;
        }

        public async Task<CommonResponseModel<User>> GetUser(int id)
        {
            var response = new CommonResponseModel<User>();

            try
            {
                User? user = _context.Users.Where(user => user.IsDeleted == false && user.Id == id).FirstOrDefault();

                if (user is not null)
                {
                    response.Data = user;
                    response.IsSuccess = true;
                    response.Message = "Successfully Retrieved User Lists";
                }
                else
                {
                    response.Data = user;
                    response.IsSuccess = true;
                    response.Message = "User not found";
                }
            }
            catch (Exception ex)
            {
                // T.B.D : Should I add null value Data
                response.IsSuccess = false;
                response.Message = $"Repo Layer  Exception :{ex.Message}";
            }
            return response;
        }

        #endregion
    }
}
