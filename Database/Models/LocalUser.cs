using AuthServer.Database.Models.Abstract;

namespace AuthServer.Database.Models;

public sealed class LocalUser : User
{
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] SaltHash { get; set; } = Array.Empty<byte>();
    public DateTime PasswordUpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LocalUserRefreshToken>? RefreshTokens { get; set; }
}