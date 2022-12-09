using AuthServer.Configuration.Abstract;

namespace AuthServer.Configuration;

public sealed class TwitchAuthConfig : AuthProviderConfig
{
    public bool ForceVerify { get; set; }
}