using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthServer.Configuration;
using AuthServer.Configuration.Abstract;
using AuthServer.Extensions;
using AuthServer.Models.Jwt;
using AuthServer.Models.UserInfo.Interfaces;
using AuthServer.Services.Abstract.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services.Abstract;

public abstract class JwtAuthService : IJwtAuthService
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

    public JwtCredential CreateJwtCredential(IUserInfo userInfo)
    {
        var securityToken = ConstructJwtSecurityToken(userInfo);
        var jwtToken = new JwtSecurityTokenHandler().WriteToken(securityToken);
        var refreshToken = StringExtensions.GenerateCryptoSafeToken();

        return new JwtCredential
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            ExpiresAt = securityToken.ValidTo
        };
    }

    protected virtual HashSet<Claim> CreateUserClaims(IUserInfo userInfo)
    {
        return new HashSet<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userInfo.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.NameId, userInfo.Name),
            new(JwtRegisteredClaimNames.Email, userInfo.EmailAddress)
        };
    }

    private JwtSecurityToken ConstructJwtSecurityToken(IUserInfo userInfo)
    {
        var claims = CreateUserClaims(userInfo);
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