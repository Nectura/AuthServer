using AuthServer.Models.OAuth;

namespace AuthServer.Services.Abstract.Interfaces;

public interface IAuthProviderService : IJwtAuthService
{
    Task<OAuthAccessTokenExchangeResponse> ExchangeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<OAuthAccessTokenExchangeResponse> ExchangeAuthCodeForAccessTokenAsync(string authCode, string redirectUrl, CancellationToken cancellationToken = default);
}