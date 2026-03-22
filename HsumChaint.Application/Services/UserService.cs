using HsumChaint.Application.ServiceInterfaces;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using HsumChaint.Infrastructure.Models;
using HsumChaint.Application.DTOs.User;

namespace HsumChaint.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        #region AddUser
        public async Task<ApplicationCommonResponseModel<UserDto>> AddUser(UserDto reqModel)
        {
            var response = new ApplicationCommonResponseModel<UserDto>();
            try
            {
                // Used AutoMapper to map UserDto to User entity
                var userEntity = _mapper.Map<User>(reqModel);
                var addResponse = await _userRepository.AddUser(userEntity);

                // Without AutoMapper
                //var addResponse = await _userRepository.AddUser(new Infrastructure.Models.User
                //{
                //    Name = reqModel.Name,
                //    PhoneNumber = reqModel.PhoneNumber,
                //});

                response.IsSuccess = addResponse.IsSuccess;
                response.Message = addResponse.Message;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }

        public async Task<ApplicationCommonResponseModel<List<UserDto>>> GetAllUsers()
        {
            var response = new ApplicationCommonResponseModel<List<UserDto>>();
            try
            {
                var addResponse = await _userRepository.GetAllUsers();

                response.IsSuccess = addResponse.IsSuccess;
                response.Message = addResponse.Message;
                response.ListData = _mapper.Map<List<UserDto>>(addResponse.ListData);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }

        public async Task<ApplicationCommonResponseModel<UserDto>> GetUser(int id)
        {
            var response = new ApplicationCommonResponseModel<UserDto>();
            try
            {
                var addResponse = await _userRepository.GetUser(id);

                response.IsSuccess = addResponse.IsSuccess;
                response.Message = addResponse.Message;
                response.Data = _mapper.Map<UserDto>(addResponse.Data);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application Layer Exception: {ex.Message}";
            }
            return response;
        }
        #endregion
    }
}
