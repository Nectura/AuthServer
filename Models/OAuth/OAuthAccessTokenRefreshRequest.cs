using Newtonsoft.Json;

namespace AuthServer.Models.OAuth;

[Serializable]
public class OAuthAccessTokenRefreshRequest
{
    [JsonProperty("client_id")] public string ClientId { get; set; } = string.Empty;
    [JsonProperty("client_secret")] public string ClientSecret { get; set; } = string.Empty;
    [JsonProperty("grant_type")] public string GrantType { get; set; } = "refresh_token";
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
}