using BaseLibrary.DTOs;
using BaseLibrary.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibrary.Services.Interfaces
{
    public interface IUserAccount
    {
        Task<GeneralResponse> CreateAsync(RegisterDTO User);
        Task<LogInResponse> SignInAsync(LogInDTO User);
    }
}
