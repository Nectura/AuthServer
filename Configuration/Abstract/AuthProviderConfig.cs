namespace AuthServer.Configuration.Abstract;

public abstract class AuthProviderConfig : JwtTokenConfig
{
    public string UserAuthorizationEndpoint { get; set; } = "";
    public string AccessTokenExchangeEndpoint { get; set; } = "";
    public string UserInfoEndpoint { get; set; } = "";
    public string AuthProviderClientId { get; set; } = "";
    public string AuthProviderClientSecret { get; set; } = "";
    public string[] MinimalAuthScopes { get; set; } = Array.Empty<string>();
    public string[] ExtendedAuthScopes { get; set; } = Array.Empty<string>();
}