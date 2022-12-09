using AuthServer.Configuration;
using AuthServer.Models.UserInfo;
using AuthServer.Models.UserInfo.Interfaces;
using AuthServer.Services.Abstract;
using AuthServer.Services.Interfaces;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace AuthServer.Services;

public sealed class SpotifyAuthService : AuthProviderService, ISpotifyAuthService
{
    public SpotifyAuthService(
        HttpClient httpClient,
        IOptions<SpotifyAuthConfig> authProviderConfig,
        IOptions<JwtAuthConfig> authConfig,
        ISocialAuthService socialAuthService) : base(httpClient, authProviderConfig, authConfig, socialAuthService)
    {
    }
    
    public override async Task<IUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var spotifyClient = new SpotifyClient(accessToken);

        var userInfo = await spotifyClient.UserProfile.Current(cancellationToken);
        
        return new SpotifyUserInfo
        {
            AuthProviderUserId = userInfo.Id,
            Name = userInfo.DisplayName,
            EmailAddress = userInfo.Email,
            ProfileImage = userInfo.Images.FirstOrDefault()?.Url ?? string.Empty
        };
    }
}