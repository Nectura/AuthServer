namespace AuthServer.Configuration;

public sealed class JwtAuthConfig
{        
    // Note: the amount of time before a JWT token is considered expired and needs to be refreshed by the user
    public static readonly TimeSpan JwtLifeSpanTillRefreshIsNeeded = TimeSpan.FromMinutes(15);

    // Note: the amount of time before a JWT token completely dies out and is no longer refreshable
    public static readonly TimeSpan JwtMaxLifeSpan = TimeSpan.FromDays(7);

    // Note: if no expiration time is provided with an api access token, then use this value
    public static readonly TimeSpan DefaultSocialLoginUserApiCredentialAccessTokenLifeSpan = TimeSpan.FromMinutes(60);

    // Note: if it expires in 60m, update after 40m
    public static readonly TimeSpan ApiTokenRefreshTimeBeforeExpiration = TimeSpan.FromMinutes(20);

    public string Issuer { get; set; } = "";
    public string PrivateTokenKey { get; set; } = "";
    public string[] Audiences { get; set; } = Array.Empty<string>();
    public bool ValidateAudience { get; set; }
    public bool ValidateIssuer { get; set; }
}