using AuthServer.Configuration;
using AuthServer.Services.Auth.Abstract;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Auth.Local;

public sealed class LocalAuthService : JwtAuthService
{
    public LocalAuthService(
        IOptions<JwtAuthConfig> authConfig,
        IOptions<LocalAuthConfig> tokenConfig) : base(authConfig, tokenConfig)
    {
    }
}