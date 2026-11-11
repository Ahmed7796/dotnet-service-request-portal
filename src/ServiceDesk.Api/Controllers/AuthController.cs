using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Api.Auth;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly PasswordHasher<User> _hasher = new();
        private readonly JwtTokenService _tokens;
        public AuthController(IUserRepository users, JwtTokenService tokens)
        {
            _users = users;
            _tokens = tokens;
        }
        // Successful Login: Generate token, return 200-OK, Else, Return 401-UNAUTHORIZED
        [HttpPost("login")][AllowAnonymous]
        public async Task<ActionResult<TokenResult>> Login(LoginDto dto, CancellationToken ct)
        {
            var user = await _users.GetByEmailOrEmployeeNumberAsync(dto.Identifier, ct);

            if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials.");

            return Ok(_tokens.CreateToken(user));   
        }
    }

    public sealed record LoginDto([Required] string Identifier,[Required] string Password);
}


