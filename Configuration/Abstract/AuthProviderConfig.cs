namespace AuthServer.Configuration.Abstract;

public abstract class AuthProviderConfig : JwtTokenConfig
{
    public string AccessTokenExchangeEndpoint { get; set; } = "";
    public string AuthProviderClientId { get; set; } = "";
    public string AuthProviderClientSecret { get; set; } = "";
}