using AuthServer.Database.Enums;
using AuthServer.Database.Models.Interfaces;
using AuthServer.Models.OAuth;
using Newtonsoft.Json;

namespace AuthServer.Database.Models;

public sealed class SocialUserAuthProviderToken : ISocialUserAuthProviderToken
{
    public Guid UserId { get; set; }
    public SocialUser? User { get; set; }
    
    public EAuthProvider AuthProvider { get; set; }
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string Scopes { get; set; } = "";
    public DateTime ExpiresAt { get; set; }

    public void UpdateFromTokenExchangeResponse(OAuthAccessTokenExchangeResponse tokenExchangeResponse)
    {
        AccessToken = tokenExchangeResponse.AccessToken;
        RefreshToken = tokenExchangeResponse.RefreshToken;
        Scopes = JsonConvert.SerializeObject(tokenExchangeResponse.Scope);
        ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds);
    }
}