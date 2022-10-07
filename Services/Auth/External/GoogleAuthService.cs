using AuthServer.Configuration;
using AuthServer.Services.Auth.Abstract;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Auth.External;

public sealed class GoogleAuthService : AuthProviderService
{
    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleAuthConfig> authProviderConfig,
        IOptions<JwtAuthConfig> authConfig) : base(httpClient, authProviderConfig, authConfig)
    {
    }
}