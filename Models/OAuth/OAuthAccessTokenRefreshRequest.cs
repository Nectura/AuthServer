using Newtonsoft.Json;

namespace AuthServer.Models.OAuth;

[Serializable]
public class OAuthAccessTokenRefreshRequest
{
    [JsonProperty("grant_type")] public string GrantType { get; set; } = "refresh_token";

    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
}