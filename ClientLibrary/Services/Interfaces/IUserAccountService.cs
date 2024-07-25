using BaseLibrary.DTOs;
using BaseLibrary.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Services.Interfaces
{
    public interface IUserAccountService
    {
        Task<GeneralResponse> CreateAsync(RegisterDTO user);
        Task<LogInResponse> SignInAsync(LogInDTO user);
        Task<LogInResponse> RefreshTokenAsync(RefreshDTO user);
        Task<WeatherForecastDTO[]> GetWeatherForecast();
    }
}
