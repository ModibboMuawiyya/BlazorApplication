using BaseLibrary.DTOs;
using ClientLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Helpers
{
    public class CustomHttpHandler(GetHttpClient getHttpClient, 
                                   LocalStorageService localStorageService,
                                   IUserAccountService accountService
                                   ) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancelToken)
        {
            bool loginUrl = request.RequestUri!.AbsoluteUri.Contains("login");
            bool registerUrl = request.RequestUri!.AbsoluteUri.Contains("register");
            bool refreshUrl = request.RequestUri!.AbsoluteUri.Contains("refresh");

            if (loginUrl || registerUrl || refreshUrl) return await base.SendAsync(request, cancelToken);

            var result = await base.SendAsync(request, cancelToken);
            if (result.StatusCode == HttpStatusCode.Unauthorized)
            {
                              
                //Get token from localStorage
                var stringToken = await localStorageService.GetToken();
                if (stringToken == null) return result;
                // Check if the Header Contains Token
                string token = string.Empty;
                try
                {
                    token = request.Headers.Authorization!.Parameter!;
                }
                catch (Exception)
                {

                    throw;
                }

                var deserializeToken = Serializations.DeserializeJsonString<UserDTO>(stringToken);
                if (deserializeToken is null) return result;
                if (string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", deserializeToken.Token);
                   return await base.SendAsync(request, cancelToken);
                }

                //Call for refresh token
                var newJwtToken = await GetRefreshToken(deserializeToken.RefreshToken!);
                if (string.IsNullOrEmpty(newJwtToken)) return result;

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newJwtToken);
                return await base.SendAsync(request, cancelToken);

            };
            return result;
        }

        private async Task<string> GetRefreshToken(string refreshToken)
        {
            var result = await accountService.RefreshTokenAsync(new RefreshDTO() { Token = refreshToken} );
            string serializedToken = Serializations.SerializeObject(
                                    new UserDTO() { Token = result.Token, RefreshToken = result.RefreshToken });
            await localStorageService.SetToken(serializedToken);
            return result.RefreshToken;


        }
    }
}
