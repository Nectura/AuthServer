using AuthServer.Services.Abstract.Interfaces;

namespace AuthServer.Services.Auth.SocialAuth.Interfaces;

public interface ISpotifyAuthService : IAuthProviderService
{
    Task<string> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);
}