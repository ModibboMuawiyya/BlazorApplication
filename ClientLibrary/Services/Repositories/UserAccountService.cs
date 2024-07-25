using BaseLibrary.DTOs;
using BaseLibrary.Responses;
using ClientLibrary.Helpers;
using ClientLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Services.Repositories
{
    public class UserAccountService(GetHttpClient getClient) : IUserAccountService
    {
        public const string AuthUrl = "api/authentication";
        public async Task<GeneralResponse> CreateAsync(RegisterDTO user)
        {
            var httpClient = getClient.GetPublicHttpClient();
            var result = await httpClient.PostAsJsonAsync($"{AuthUrl}/register", user);
            if (!result.IsSuccessStatusCode) return new GeneralResponse(false, "Error Occured");

            return await result.Content.ReadFromJsonAsync<GeneralResponse>()!;
        }

        public async Task<LogInResponse> SignInAsync(LogInDTO user)
        {
            var httpClient = getClient.GetPublicHttpClient();
            var result = await httpClient.PostAsJsonAsync($"{AuthUrl}/login", user);
            if (!result.IsSuccessStatusCode) return new LogInResponse(false, "Error Occured");

            return await result.Content.ReadFromJsonAsync<LogInResponse>()!;
        }

        public Task<LogInResponse> RefreshTokenAsync(RefreshDTO user)
        {
            throw new NotImplementedException();
        } 
        public async Task<WeatherForecastDTO[]> GetWeatherForecast()
        {
            var httpClient = await getClient.GetPrivateHttpClient();
            var result = await httpClient.GetFromJsonAsync<WeatherForecastDTO[]>("api/weatherforecast");
            return result;
        }

        

        
    }
}
