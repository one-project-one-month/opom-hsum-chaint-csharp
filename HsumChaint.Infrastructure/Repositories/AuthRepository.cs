using Azure;
using HsumChaint.Infrastructure.Helpers;
using HsumChaint.Infrastructure.Models;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Infrastructure.Repositories
{
    public class AuthRepository(AppDbContext _dbContext, AuthHelper _helper) : IAuthRepository
    {

        #region Register
        public async Task<CommonResponseModel<User>> Register(User requestModel)
        {
            var response = new CommonResponseModel<User>();
            try
            {

                requestModel.CreatedAt = DateTime.UtcNow;
                requestModel.IsDeleted = false;

                await _dbContext.Users.AddAsync(requestModel);

                await _dbContext.SaveChangesAsync();

                response.Data = new User
                {
                    Id = requestModel.Id
                };
                response.IsSuccess = true;
                response.Message = "Register Successful";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<CommonResponseModel<MonkProfile>> RegisterMonkProfile(MonkProfile requestModel)
        {
            var response = new CommonResponseModel<MonkProfile>();
            try
            {
                await _dbContext.MonkProfiles.AddAsync(requestModel);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Register Successful";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }

        #endregion

        #region GetUserByPhoneNumber
        public async Task<CommonResponseModel<User>> GetUserByPhoneNumber(string phoneNumber)
        {
            var response = new CommonResponseModel<User>();

            try
            {
                var userModel = await _dbContext.Users
                    .Where(x => x.PhoneNumber == phoneNumber && x.IsDeleted == false)
                    .FirstOrDefaultAsync();

                response.IsSuccess = true;
                response.Message = "Success Getting User By Phone Number";
                response.Data = userModel;
            }
            catch (Exception e)
            {
                response.IsSuccess = false;
                response.Message = e.Message;
            }
            return response;
        }
        #endregion

        #region AddOrUpdateRefreshToken
        public async Task<CommonResponseModel<RefreshToken>> AddOrUpdateRefreshToken(RefreshToken reqModel)
        {
            var response = new CommonResponseModel<RefreshToken>();
            try
            {
                var exsitingUserInRefreshToken = await _dbContext.RefreshTokens.Where(x => x.UserId == reqModel.UserId).FirstOrDefaultAsync();

                if (exsitingUserInRefreshToken == null)
                {
                    reqModel.CreatedAt = DateTime.UtcNow;

                    await _dbContext.AddAsync(reqModel);
                }
                else
                {
                    exsitingUserInRefreshToken.RefreshToken1 = reqModel.RefreshToken1;
                    exsitingUserInRefreshToken.ExpiresAt = reqModel.ExpiresAt;
                }

                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Refresh token saved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"repo layer err:{ex.Message} {ex.InnerException}";
            }
            return response;
        }
        #endregion

        #region GetRefreshTokenByUserId
        public async Task<CommonResponseModel<RefreshToken>> GetRefreshTokenByUserId(int userId)
        {
            var response = new CommonResponseModel<RefreshToken>();

            try
            {
                var exsitingUserInRefreshToken = await _dbContext.RefreshTokens.Where(x => x.UserId == userId).FirstOrDefaultAsync();

                response.IsSuccess = true;
                response.Message = "Get By user Id done";
                response.Data = exsitingUserInRefreshToken;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
        #endregion

        #region GetUserById
        public async Task<CommonResponseModel<User>> GetUserById(int userId)
        {
            var response = new CommonResponseModel<User>();

            try
            {
                var userModel = await _dbContext.Users
                    .Where(x => x.Id == userId && x.IsDeleted == false)
                    .FirstOrDefaultAsync();

                response.IsSuccess = true;
                response.Message = "Success Getting User By id";
                response.Data = userModel;
            }
            catch (Exception e)
            {
                response.IsSuccess = false;
                response.Message = e.Message;
            }
            return response;
        }
        #endregion
    }
}
