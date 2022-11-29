using AuthServer.Configuration;
using AuthServer.Services.Abstract;
using AuthServer.Services.Auth.SocialAuth.Interfaces;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Auth.SocialAuth;

public sealed class GoogleAuthService : AuthProviderService, IGoogleAuthService
{
    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleAuthConfig> authProviderConfig,
        IOptions<JwtAuthConfig> authConfig) : base(httpClient, authProviderConfig, authConfig)
    {
    }
}