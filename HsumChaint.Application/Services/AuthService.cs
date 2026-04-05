using Azure;
using HsumChaint.Application.DTOs.Auth;
using HsumChaint.Application.ServiceInterfaces;
using HsumChaint.Infrastructure.Models;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HsumChaint.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        public AuthService(IAuthRepository authRepo, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
        {
            _authRepo = authRepo;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        #region Register
        public async Task<ApplicationCommonResponseModel<RegisterResponseDto>> Register(RegisterRequestDto reqModel)
        {
            var response = new ApplicationCommonResponseModel<RegisterResponseDto>();
            try
            {
                var existingUser = await _authRepo.GetUserByPhoneNumber(reqModel.PhoneNumber);

                #region Phone Number Duplicate validation

                if (existingUser != null && existingUser.Data != null)
                {
                    response.IsSuccess = false;
                    response.Message = "User with this phone number already exists";
                    return response;
                }
                #endregion


                User user = new User();
                var hashedPassword = _passwordHasher.HashPassword(user, reqModel.Password);

                reqModel.Password = hashedPassword;

                var registerResponse = await _authRepo.Register(new User
                {
                    Name = reqModel.Name,
                    PhoneNumber = reqModel.PhoneNumber,
                    Password = reqModel.Password,
                    UserType = reqModel.UserType,
                    Email = reqModel.Email,
                    ContactPhoneNumber = reqModel.ContactPhoneNumber
                });

                bool hasMonasteryInfo = !string.IsNullOrEmpty(reqModel.MonasteryName) || !string.IsNullOrEmpty(reqModel.MonasteryAddress);

                // Reigseter Monk Profile
                if (reqModel.UserType == Common.CommonEnum.UserType.Monk && registerResponse.IsSuccess == true && hasMonasteryInfo)
                {
                    MonkProfile monkProfile = new MonkProfile();

                    var registerMonkProfileResponse = await _authRepo.RegisterMonkProfile(new MonkProfile
                    {
                        UserId = registerResponse.Data.Id,
                        MonasteryName = reqModel.MonasteryName,
                        MonasteryAddress = reqModel.MonasteryAddress,
                    });

                    response.IsSuccess = registerMonkProfileResponse.IsSuccess;
                    response.Message = registerResponse.Message+"\n" + registerMonkProfileResponse.Message;
                }
                else
                {
                    response.IsSuccess = registerResponse.IsSuccess;
                    response.Message = registerResponse.Message;
                }

                return response;

            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"application layer err: {ex.Message} {ex.InnerException}";
            }
            return response;
        }
        #endregion

        #region Login
        public async Task<ApplicationCommonResponseModel<LoginResponseDto>> Login(LoginRequestDto reqModel)
        {
            var response = new ApplicationCommonResponseModel<LoginResponseDto>();

            try
            {
                var existingUser = await _authRepo.GetUserByPhoneNumber(reqModel.PhoneNumber);

                if (existingUser == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Phone number or password incorrect!";
                    return response;
                }

                User user = new User();
                if (_passwordHasher.VerifyHashedPassword(user, existingUser.Data.Password, reqModel.Password)
                    == PasswordVerificationResult.Failed)
                {
                    response.IsSuccess = false;
                    response.Message = "Phone number or password incorrect!";
                    return response;
                }

                string Token = this.GenerateToken(existingUser.Data.PhoneNumber, existingUser.Data.UserType.ToString()); // UserType is changed to enum
                string refreshToken = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = existingUser.Data.Id });

                response.IsSuccess = true;
                response.Message = "Login Successful";
                response.Data = new LoginResponseDto
                {
                    AccessToken = Token,
                    UserType = existingUser.Data.UserType,
                    ID = existingUser.Data.Id,
                    RefreshToken = refreshToken
                };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application layer err: {ex.Message} {ex.InnerException}";
            }
            return response;
        }
        #endregion

        #region RefreshTokens
        public async Task<ApplicationCommonResponseModel<LoginResponseDto>> RefreshTokens(RefreshTokenRequestDto request)
        {
            var response = new ApplicationCommonResponseModel<LoginResponseDto>();

            try
            {
                var isValidRefreshToken = await this.IsValidRefreshToken(request.UserId, request.RefreshToken);

                if (isValidRefreshToken)
                {
                    var existingUser = await _authRepo.GetUserById(request.UserId);

                    if (existingUser == null || existingUser.Data == null)
                    {
                        response.IsSuccess = false;
                        response.Message = "User not found";
                        return response;
                    }

                    string Token = this.GenerateToken(existingUser.Data.PhoneNumber, existingUser.Data.UserType.ToString()); // UserType is changed to enum
                    string refreshToken = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = existingUser.Data.Id });

                    response.IsSuccess = true;
                    response.Message = "Successful";
                    response.Data = new LoginResponseDto
                    {
                        AccessToken = Token,
                        UserType = existingUser.Data.UserType,
                        ID = existingUser.Data.Id,
                        RefreshToken = refreshToken
                    };
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid or expired Refresh Token";
                }

            }
            catch(Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"application layer err: {ex.Message}";
            }

            return response;
        }
        #endregion

        #region GenerateToken
        private string GenerateToken(string phoneNumber, string userType)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.MobilePhone, phoneNumber),
                new Claim(ClaimTypes.Role, userType)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtConfig:Key"]!)
                );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                    issuer: _configuration["JwtConfig:Issuer"],
                    audience: _configuration["JwtConfig:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(1),
                    signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
        #endregion

        #region GenerateAndSaveRefreshToken
        public async Task<string> GenerateAndSaveRefreshToken(GenerateRefreshTokenDto reqModel)
        {
            var refreshToken = this.GenerateRefreshToken();

            reqModel.RefreshToken = refreshToken;
            reqModel.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _authRepo.AddOrUpdateRefreshToken(new RefreshToken
            {
                UserId = reqModel.UserId,
                RefreshToken1 = refreshToken,
                ExpiresAt = reqModel.ExpiresAt,
            });

            return refreshToken;
        }
        #endregion

        #region GenerateRefreshToken
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }
        #endregion

        #region IsValidRefreshToken
        private async Task<bool> IsValidRefreshToken(int userId, string refreshToken)
        {
            var tokenModel = await _authRepo.GetRefreshTokenByUserId(userId);

            if (tokenModel == null || tokenModel.Data == null || tokenModel.Data.RefreshToken1 != refreshToken || tokenModel.Data.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }
        #endregion

    }
}
