using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task<LogInResponse> SignInAsync(LogInDTO User)
        {
            throw new NotImplementedException();
        }
    }
}
