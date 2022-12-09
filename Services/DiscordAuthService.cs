using AuthServer.Configuration;
using AuthServer.Models.UserInfo.Interfaces;
using AuthServer.Services.Abstract;
using AuthServer.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

public sealed class DiscordAuthService : AuthProviderService, IDiscordAuthService
{
    public DiscordAuthService(
        HttpClient httpClient,
        IOptions<DiscordAuthConfig> authProviderConfig,
        IOptions<JwtAuthConfig> authConfig,
        ISocialAuthService socialAuthService) : base(httpClient, authProviderConfig, authConfig, socialAuthService)
    {
    }
    
    public override Task<IUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}