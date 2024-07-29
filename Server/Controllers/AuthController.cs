using BaseLibrary.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Services.Interfaces;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController(IUserAccount account) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult>CreateAsync(RegisterDTO user)
        {
            if (user == null) return BadRequest("Model is Empty");
            var result = await account.CreateAsync(user);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> SignInAsync(LogInDTO user)
        {
            if (user is null) return BadRequest("Model is Empty");
            var result = await account.SignInAsync(user);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync(RefreshDTO token)
        {
            if (token is null) return BadRequest("Model is Empty");
            var result = await account.RefreshTokenAsync(token);
            return Ok(result);
        }

    }
}
