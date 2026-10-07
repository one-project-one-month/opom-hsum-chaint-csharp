using HsumChaint.Shared;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;
using HsumChaint.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserEntity = HsumChaint.Database.Models.User;

namespace HsumChaint.Domain.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHasher<UserEntity> _passwordHasher;
        private readonly JwtOptions _jwtOptions;
        private readonly IRolePermissionService _rolePermissionService;

        public AuthService(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher, IOptions<JwtOptions> jwtOptions, IRolePermissionService rolePermissionService)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _jwtOptions = jwtOptions.Value;
            _rolePermissionService = rolePermissionService;
        }

        #region Register
        public async Task<Result> Register(RegisterRequestDto reqModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reqModel.Name) || string.IsNullOrWhiteSpace(reqModel.PhoneNumber) || string.IsNullOrWhiteSpace(reqModel.Password))
                {
                    return Result.Failure("Name, phone number and password are required.");
                }
                var existingUser = await _dbContext.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(x => x.PhoneNumber == reqModel.PhoneNumber && x.IsDeleted == false);

                #region Phone Number Duplicate validation
                if (existingUser != null)
                {
                    return Result.Failure("User with this phone number already exists");
                }
                #endregion

                var selectedRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == reqModel.RoleId && !r.IsDeleted);
                if (selectedRole == null)
                {
                    return Result.Failure("Role not found.");
                }
                UserEntity user = new UserEntity();
                var hashedPassword = _passwordHasher.HashPassword(user, reqModel.Password);

                var registerUser = new UserEntity
                {
                    Name = reqModel.Name!,
                    PhoneNumber = reqModel.PhoneNumber!,
                    Password = hashedPassword,
                    RoleId = reqModel.RoleId,
                    Email = reqModel.Email,
                    ContactPhoneNumber = reqModel.ContactPhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _dbContext.Users.AddAsync(registerUser);
                await _dbContext.SaveChangesAsync();

                // Create a profile for the selected Monk role.
                if (selectedRole.Name == "Monk")
                {
                    await _dbContext.MonkProfiles.AddAsync(new MonkProfile
                    {
                        UserId = registerUser.Id,
                        MonasteryName = reqModel.MonasteryName,
                        MonasteryAddress = reqModel.MonasteryAddress,
                    });
                    await _dbContext.SaveChangesAsync();

                }

                return Result.Success("Register Successful");
            }
            catch (Exception ex)
            {
                return Result.Failure($"application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion

        #region Login
        public async Task<Result<LoginResponseDto>> Login(LoginRequestDto reqModel)
        {
            try
            {
                var existingUser = await _dbContext.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(x => x.PhoneNumber == reqModel.PhoneNumber && x.IsDeleted == false);

                if (existingUser == null || existingUser.Role == null || existingUser.Role.IsDeleted)
                {
                    return Result<LoginResponseDto>.Failure("Phone number or password incorrect!");
                }

                UserEntity user = new UserEntity();
                if (_passwordHasher.VerifyHashedPassword(user, existingUser.Password, reqModel.Password)
                    == PasswordVerificationResult.Failed)
                {
                    return Result<LoginResponseDto>.Failure("Phone number or password incorrect!");
                }

                var permissions = await _rolePermissionService.GetUserPermissions(existingUser.Id);
                string Token = this.GenerateToken(existingUser.Id, existingUser.PhoneNumber, existingUser.Role.Name, permissions);
                string refreshToken = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = existingUser.Id });

                return Result<LoginResponseDto>.Success(new LoginResponseDto
                {
                    AccessToken = Token,
                    RoleId = existingUser.RoleId,
                    RoleName = existingUser.Role.Name,
                    Permissions = permissions,
                    ID = existingUser.Id,
                    RefreshToken = refreshToken
                }, "Login Successful");
            }
            catch (Exception ex)
            {
                return Result<LoginResponseDto>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion

        #region RefreshTokens
        public async Task<Result<LoginResponseDto>> RefreshTokens(RefreshTokenRequestDto request)
        {
            try
            {
                var isValidRefreshToken = await this.IsValidRefreshToken(request.UserId, request.RefreshToken);

                if (isValidRefreshToken)
                {
                    var existingUser = await _dbContext.Users.Include(u => u.Role)
                        .FirstOrDefaultAsync(x => x.Id == request.UserId && x.IsDeleted == false);

                    if (existingUser == null || existingUser.Role == null || existingUser.Role.IsDeleted)
                    {
                        return Result<LoginResponseDto>.Failure("User not found");
                    }

                    var permissions = await _rolePermissionService.GetUserPermissions(existingUser.Id);
                    string Token = this.GenerateToken(existingUser.Id, existingUser.PhoneNumber, existingUser.Role.Name, permissions);
                    string refreshToken = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = existingUser.Id });

                    return Result<LoginResponseDto>.Success(new LoginResponseDto
                    {
                        AccessToken = Token,
                        RoleId = existingUser.RoleId,
                        RoleName = existingUser.Role.Name,
                        Permissions = permissions,
                        ID = existingUser.Id,
                        RefreshToken = refreshToken
                    }, "Successful");
                }
                else
                {
                    return Result<LoginResponseDto>.Failure("Invalid or expired Refresh Token");
                }
            }
            catch (Exception ex)
            {
                return Result<LoginResponseDto>.Failure($"application layer err: {ex.Message}");
            }
        }
        #endregion

        #region GenerateToken
        private string GenerateToken(int userId, string phoneNumber, string roleName, List<string> permissions)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.MobilePhone, phoneNumber),
                new Claim(ClaimTypes.Role, roleName)
            };

            claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Key ?? string.Empty)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                    issuer: _jwtOptions.Issuer,
                    audience: _jwtOptions.Audience,
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

            var existingRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.UserId == reqModel.UserId);

            if (existingRefreshToken == null)
            {
                await _dbContext.RefreshTokens.AddAsync(new RefreshToken
                {
                    UserId = reqModel.UserId,
                    RefreshToken1 = refreshToken,
                    ExpiresAt = reqModel.ExpiresAt,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingRefreshToken.RefreshToken1 = refreshToken;
                existingRefreshToken.ExpiresAt = reqModel.ExpiresAt;
            }

            await _dbContext.SaveChangesAsync();

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
            var tokenModel = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (tokenModel == null || tokenModel.RefreshToken1 != refreshToken || tokenModel.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
