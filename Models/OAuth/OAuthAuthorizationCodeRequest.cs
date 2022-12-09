using Newtonsoft.Json;

namespace AuthServer.Models.OAuth;

[Serializable]
public class OAuthAuthorizationCodeRequest
{
    [JsonProperty("response_type")] public string ResponseType { get; set; } = "code";
    [JsonProperty("client_id")] public string ClientId { get; set; } = string.Empty;
    [JsonProperty("redirect_uri")] public string RedirectUri { get; set; } = string.Empty;
    [JsonProperty("scope")] public string Scope { get; set; } = string.Empty;
    [JsonProperty("state")] public string State { get; set; } = string.Empty;
    [JsonProperty("nonce")] public string Nonce { get; set; } = string.Empty;
}