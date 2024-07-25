using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibrary.Services.Repositories
{
    public class UserAccountRepository(IOptions<JWTSettings> config, AppDbContext appDbContext) : IUserAccount
    {
        public async Task<GeneralResponse> CreateAsync(RegisterDTO user)
        {
            if (user is null) return new GeneralResponse(false, "Model is Empty");

            var chkUser = await FindUserByEmail(user.Email);
            if (chkUser is not null) return new GeneralResponse(false, "An account with this Email Exist");

            var newUsr = await AddToDb(new ApplicationUser()
            {
                Email = user.Email,
                Name = user.FullName,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            });

            var chkAdminRole = await appDbContext.SystemRoles
                            .FirstOrDefaultAsync(x => x.Name!.Equals(Constants.Admin));
            if(chkAdminRole is null)
            {
                var createAdminRole = await AddToDb(new SystemRole() { Name = Constants.Admin });
                await AddToDb(new UserRole() { RoleId = createAdminRole.Id, UserId = newUsr.Id });
                return new GeneralResponse(true, "Admin Created");
            }

            var chkUserRole = await appDbContext.SystemRoles
                            .FirstOrDefaultAsync(x => x.Name!.Equals(Constants.User));
            SystemRole response = new();
            if (chkUserRole is null)
            {
                response = await AddToDb(new SystemRole() { Name = Constants.User });
                await AddToDb(new UserRole() { RoleId = response.Id, UserId = newUsr.Id });
                return new GeneralResponse(true, "User Created");
            }
            else
            {
                await AddToDb(new UserRole() { RoleId = chkUserRole.Id, UserId = newUsr.Id });
            }
            return new GeneralResponse(true, "User Created");


        }

        private async Task<T> AddToDb<T>(T model)
        {
            var result = appDbContext.Add(model!);
            await appDbContext.SaveChangesAsync();
            return (T)result.Entity;
        }

        private async Task<ApplicationUser> FindUserByEmail(string email)
        {
           var usr =  await appDbContext.ApplicationUsers
                .FirstOrDefaultAsync(x => x.Email!.ToLower()!.Equals(email!.ToLower()));
            return usr;
        }

        public async Task<LogInResponse> SignInAsync(LogInDTO user)
        {
            if (user is null) return new LogInResponse(false, "Model is Empty");

            var appUser = await FindUserByEmail(user.Email!);
            if (appUser is null) return new LogInResponse(false, "User not found");

            if (!BCrypt.Net.BCrypt.Verify(user.Password, appUser.Password))
                return new LogInResponse(false, "Email/Password not valid");

            var getUserRole = await FindUserRole(appUser.Id);
            if (getUserRole is null) return new LogInResponse(false, "User Role not Found");

            var getRoleName = await  FindRoleName(getUserRole.RoleId);
            if (getRoleName is null) return new LogInResponse(false, "No Role was Found");

            string jwtToken = GenerateToken(appUser, getRoleName!.Name);
            string refreshToken = GenerateRefreshToken();

            var findUser = await appDbContext.RefreshTokenInfos
                .FirstOrDefaultAsync(x => x.UserId == appUser.Id);
            if (findUser is not null)
            {
                findUser!.Token = refreshToken;
                await appDbContext.SaveChangesAsync();
            }
            else
            {
                await AddToDb(new RefreshTokenInfo() { Token = refreshToken, UserId = appUser.Id });
            }
            return new LogInResponse(true, "LogIn Successful", jwtToken, refreshToken);

        }

        private string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        private string GenerateToken(ApplicationUser appUser, string? role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.SecretKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, appUser.Id.ToString()),
                new Claim(ClaimTypes.Name, appUser.Name!),
                new Claim(ClaimTypes.Email, appUser.Email!),
                new Claim(ClaimTypes.Role, role!)
            };
            var token = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: userClaims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserRole> FindUserRole(int userId) => 
            await appDbContext.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId);
        private async Task<SystemRole> FindRoleName(int roleId) =>
            await appDbContext.SystemRoles.FirstOrDefaultAsync(x => x.Id == roleId);
        public async Task<LogInResponse> RefreshTokenAsync(RefreshDTO token)
        {
            if (token is null) return new LogInResponse(false, "Model is empty");

            var findToken = await appDbContext.RefreshTokenInfos
                .FirstOrDefaultAsync(x => x.Token!.Equals(token.Token));
            if (findToken is null) return new LogInResponse(false, "Refresh token is required");

            var user = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == findToken.UserId);
            if (user is null) return new LogInResponse(false, "Refresh token could not be generated because user not found");

            var userRole = await FindUserRole(user.Id);
            var roleName = await FindRoleName(userRole.RoleId);
            string jwtToken = GenerateToken(user, roleName.Name!);
            string refreshToken = GenerateRefreshToken();

            var updateRefreshToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (updateRefreshToken is null) return new 
                    LogInResponse(false, "Refresh token could not be generated because user has not signed in");

            updateRefreshToken.Token = refreshToken;
            await appDbContext.SaveChangesAsync();

            return new LogInResponse(true, "Token refreshed successfully",jwtToken, refreshToken);
        }
    }
}
