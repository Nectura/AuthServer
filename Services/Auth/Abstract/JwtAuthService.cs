using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthServer.Configuration;
using AuthServer.Configuration.Abstract;
using AuthServer.Database.Models.Abstract;
using AuthServer.Extensions;
using AuthServer.Models.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services.Auth.Abstract;

public abstract class JwtAuthService
{
    private readonly JwtAuthConfig _authConfig;
    private readonly JwtTokenConfig _tokenConfig;

    protected JwtAuthService(
        IOptions<JwtAuthConfig> authConfig,
        IOptions<JwtTokenConfig> tokenConfig)
    {
        _authConfig = authConfig.Value;
        _tokenConfig = tokenConfig.Value;
    }

    public JwtCredential CreateJwtCredential(User user)
    {
        var securityToken = ConstructJwtSecurityToken(user);
        var jwtToken = new JwtSecurityTokenHandler().WriteToken(securityToken);
        var refreshToken = StringExtensions.GenerateCryptoSafeToken();

        return new JwtCredential
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            ExpiresAt = securityToken.ValidTo
        };
    }

    private JwtSecurityToken ConstructJwtSecurityToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authConfig.PrivateTokenKey));

        var securityToken = new JwtSecurityToken(
            issuer: _authConfig.Issuer,
            audience: _tokenConfig.JwtAudience,
            expires: DateTime.UtcNow.Add(JwtAuthConfig.JwtLifeSpanTillRefreshIsNeeded),
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return securityToken;
    }
}