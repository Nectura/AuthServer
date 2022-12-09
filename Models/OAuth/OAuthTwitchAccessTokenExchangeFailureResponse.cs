using Newtonsoft.Json;

namespace AuthServer.Models.OAuth;

[Serializable]
public class OAuthTwitchAccessTokenExchangeFailureResponse
{
    [JsonProperty("error")] public string Error { get; set; } = string.Empty;
    [JsonProperty("message")] public string Message { get; set; } = string.Empty;
    [JsonProperty("status")] public uint Status { get; set; }
}