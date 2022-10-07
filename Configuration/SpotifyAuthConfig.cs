using AuthServer.Configuration.Abstract;

namespace AuthServer.Configuration;

public sealed class SpotifyAuthConfig : AuthProviderConfig
{
    public string UserMeEndpoint { get; set; } = "";
}