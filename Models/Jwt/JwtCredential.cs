namespace AuthServer.Models.Jwt;

public sealed class JwtCredential
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}