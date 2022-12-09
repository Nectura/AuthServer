namespace AuthServer.Models.Structs;

public readonly record struct SocialAuthRequest
{
    public string Nonce { get; init; }
    public DateTime ExpiresAt { get; init; }

    public bool HasExpired => DateTime.UtcNow >= ExpiresAt;
}