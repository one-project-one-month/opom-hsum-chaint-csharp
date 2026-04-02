using HsumChaint.Infrastructure.Models;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using HsumChaint.Infrastructure.Validations;

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

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
        #endregion

        #region GetUserList
        public async Task<CommonResponseModel<List<User>>> GetAllUsers()
        {
            var response = new CommonResponseModel<List<User>>();

            try
            {
                List<User> userList = await _context.Users.Where(user => user.IsDeleted == false).ToListAsync();

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
        #endregion

        #region GetUserById
        public async Task<CommonResponseModel<User>> GetUser(int id)
        {
            var response = new CommonResponseModel<User>();

            try
            {
                User? user = await _context.Users.Where(user => user.IsDeleted == false && user.Id == id).FirstOrDefaultAsync();

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

        #region UpdateUser
        public async Task<CommonResponseModel<User>> PutUser(User user)
        {
            var response = new CommonResponseModel<User>();

            try
            {
                // ✅ Reuse GetUser instead of rewriting query
                var existingUserResponse = await GetUser(user.Id);

                if (existingUserResponse.Data is not null)
                {
                    var updatedUser = existingUserResponse.Data; // First get the existing user data

                    string errorMessage;
                    var validator = new UserValidation();
                    bool userValidation = validator.ValidateForUserUpdate(user, out errorMessage);

                    if (!userValidation)
                    {
                        response.Data = null;
                        response.IsSuccess = false;
                        response.Message = $"User data validation failed: {errorMessage}";
                        return response; // Exit early if validation fails   
                    }

                    updatedUser = user; // Assign the new user data to the existing user object
                    _context.Users.Update(updatedUser);
                    var result = await _context.SaveChangesAsync();

                    if(result <= 0)
                    {
                        response.Data = null;
                        response.IsSuccess = false;
                        response.Message = "Failed to update user data.";
                        return response; // Exit early if save operation fails
                    }

                    response.Data = null;
                    response.IsSuccess = true;
                    response.Message = "User updated successfully.";
                }
                else
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.Message = "The requested user data for update could not be found.";
                }
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.IsSuccess = false;
                response.Message = $"Repo Layer Exception: {ex.Message}";
            }

            return response;
        }
        #endregion

    }
}
