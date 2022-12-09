using AuthServer.Models.OAuth;
using AuthServer.Models.UserInfo.Interfaces;

namespace AuthServer.Services.Abstract.Interfaces;

public interface IAuthProviderService : IJwtAuthService
{
    Task<OAuthAccessTokenExchangeResponse> ExchangeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<OAuthAccessTokenExchangeResponse> ExchangeAuthCodeForAccessTokenAsync(string authCode, string redirectUrl, CancellationToken cancellationToken = default);
    Task<IUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);
    string GetAuthorizationUrl(string redirectUrl, bool useExtendedScopes);
}