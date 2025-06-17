using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public interface ITokenService
{
    string CreateAccessToken(AuthRequestInfo info);
    string CreateIdToken(AuthRequestInfo info, string nonce);
}

public class TokenService : ITokenService
{
    private readonly AuthConfig _config;
    private readonly SecurityKey _key;

    public TokenService(IOptions<AuthConfig> configOptions, SecurityKey key)
    {
        _config = configOptions.Value;
        _key = key;
    }

    public string CreateAccessToken(AuthRequestInfo info)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new[]
        {
            new Claim("sub", info.UserId),
            new Claim("name", info.Username),
            new Claim("email", info.Email),
            new Claim(ClaimTypes.Name, info.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _config.Issuer,
            Audience = _config.ClientId,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.UtcDateTime.AddMinutes(_config.AccessTokenLifetimeMinutes),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public string CreateIdToken(AuthRequestInfo info, string nonce)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new[]
        {
            new Claim("sub", info.UserId),
            new Claim("name", info.Username),
            new Claim("email", info.Email),
            new Claim(ClaimTypes.Name, info.Username),
            new Claim(JwtRegisteredClaimNames.Iss, _config.Issuer),
            new Claim(JwtRegisteredClaimNames.Aud, _config.ClientId),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Nbf, now.ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, now.AddMinutes(_config.AccessTokenLifetimeMinutes).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Nonce, nonce),
            new Claim(JwtRegisteredClaimNames.AuthTime, now.ToUnixTimeSeconds().ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.UtcDateTime.AddMinutes(_config.AccessTokenLifetimeMinutes),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
            Issuer = _config.Issuer,
            Audience = _config.ClientId
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
