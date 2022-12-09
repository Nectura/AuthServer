using AuthServer.Database.Enums;
using AuthServer.Models.OAuth;

namespace AuthServer.Database.Models.Interfaces;

public interface ISocialUserAuthProviderToken
{
    Guid UserId { get; set; }
    SocialUser? User { get; set; }
    EAuthProvider AuthProvider { get; set; }
    string AccessToken { get; set; }
    string RefreshToken { get; set; }
    string Scopes { get; set; }
    DateTime ExpiresAt { get; set; }

    void UpdateFromTokenExchangeResponse(OAuthAccessTokenExchangeResponse tokenExchangeResponse);
}