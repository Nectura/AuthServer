using AuthServer.Database.Models.Abstract;
using AuthServer.Database.Models.Interfaces;

namespace AuthServer.Database.Models;

public sealed class LocalUser : User, ILocalUser
{
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] SaltHash { get; set; } = Array.Empty<byte>();
    public string? ProfilePicture { get; set; }
    
    public ICollection<LocalUserRefreshToken>? RefreshTokens { get; set; }
}