using AuthServer.Configuration;
using AuthServer.Services.Abstract;
using AuthServer.Services.Auth.LocalAuth.Interfaces;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Auth.LocalAuth;

public sealed class LocalAuthService : JwtAuthService, ILocalAuthService
{
    public LocalAuthService(
        IOptions<JwtAuthConfig> authConfig,
        IOptions<LocalAuthConfig> tokenConfig) : base(authConfig, tokenConfig)
    {
    }
}