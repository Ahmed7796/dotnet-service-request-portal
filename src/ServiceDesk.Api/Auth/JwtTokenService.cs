using Microsoft.IdentityModel.Tokens; 
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;  
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Api.Auth;

public sealed class JwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly SymmetricSecurityKey _key;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IConfiguration config, TimeProvider timeProvider)
    {
        _issuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        
        _audience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured.");
        
        _expiryMinutes = config.GetValue<int>("Jwt:ExpiryMinutes");

        var base64Key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
        _key = new SymmetricSecurityKey(Convert.FromBase64String(base64Key));

        _timeProvider = timeProvider;
    }

    public TokenResult CreateToken(User user)
    {
        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_expiryMinutes);

        Claim[] claims =
        [
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new("name", user.DisplayName),
            new("role", user.Role.ToString()),
        ];

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _issuer,
            Audience = _audience,
            IssuedAt = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new TokenResult(token, expires);
    }

}
    public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt);


