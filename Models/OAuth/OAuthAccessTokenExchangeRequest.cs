using Newtonsoft.Json;

namespace AuthServer.Models.OAuth;

[Serializable]
public class OAuthAccessTokenExchangeRequest
{
    [JsonProperty("grant_type")] public string GrantType { get; set; } = "authorization_code";

    [JsonProperty("code")] public string Code { get; set; } = string.Empty;

    [JsonProperty("redirect_uri")] public string RedirectUri { get; set; } = string.Empty;
}