using BaseLibrary.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Helpers
{
    public class CustomAuthenticationStateProvider(LocalStorageService localStorage) : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal anonymous = new(new ClaimsIdentity());
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var stringToken = await localStorage.GetToken();
            if (string.IsNullOrEmpty(stringToken)) return await Task.FromResult(new AuthenticationState(anonymous));

            var deserializationToken = Serializations.DeserializeJsonString<UserDTO>(stringToken);
            if (deserializationToken == null) return await Task.FromResult(new AuthenticationState(anonymous));

            var getUserClaims = DecryptToken(deserializationToken.Token!);
            if (getUserClaims == null) return await Task.FromResult(new AuthenticationState(anonymous));

            var claimsPrincipal = SetClaimPrincipal(getUserClaims);
            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }

        public async Task UpdateAuthenticationState(UserDTO session)
        {
            var claimsPrincipal = new ClaimsPrincipal();
            if(session.Token is not null || session.RefreshToken is not null)
            {
                var serializeSession = Serializations.SerializeObject(session);
                await localStorage.SetToken(serializeSession);
                var getUserClaims = DecryptToken(session.Token!);
                claimsPrincipal = SetClaimPrincipal(getUserClaims);
            }
            else
            {
                await localStorage.RemoveToken();
            }
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        private static CustomUserClaims DecryptToken(string jwt)
        {
            if (string.IsNullOrEmpty(jwt)) return new CustomUserClaims();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            //var userId = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            //var name = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
            //var email = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email);
            //var role = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role);

            string GetClaimValue(string claimType)
            {
                return token.Claims.FirstOrDefault(x => x.Type == claimType)?.Value!;
            }

            string userId = GetClaimValue(ClaimTypes.NameIdentifier);
            string name = GetClaimValue(ClaimTypes.Name);
            string email = GetClaimValue(ClaimTypes.Email);
            string role = GetClaimValue(ClaimTypes.Role);

            return new CustomUserClaims(userId, name, email, role);
        }

        private ClaimsPrincipal SetClaimPrincipal(CustomUserClaims claims)
        {
            if (claims.Email is null) return new ClaimsPrincipal();
            return new ClaimsPrincipal(new ClaimsIdentity(
                new List<Claim>
                {
                    new (ClaimTypes.NameIdentifier, claims.Id),
                    new (ClaimTypes.Name, claims.Name),
                    new (ClaimTypes.Email, claims.Email),
                    new (ClaimTypes.Role, claims.Role),
                }, "JwtAuth"));
        }
    }
}
