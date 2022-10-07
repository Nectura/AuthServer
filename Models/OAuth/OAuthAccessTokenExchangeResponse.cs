using Newtonsoft.Json;

namespace AuthServer.Models.OAuth;

[Serializable]
public class OAuthAccessTokenExchangeResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("token_type")] public string TokenType { get; set; } = string.Empty;

    [JsonProperty("scope")] public string Scope { get; set; } = string.Empty;

    [JsonProperty("expires_in")] public int ExpiresInSeconds { get; set; }

    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
}