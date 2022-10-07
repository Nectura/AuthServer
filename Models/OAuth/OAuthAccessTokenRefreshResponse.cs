using Newtonsoft.Json;

namespace AuthServer.Models.OAuth;

[Serializable]
public class OAuthAccessTokenRefreshResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }

    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
}