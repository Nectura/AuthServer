using AuthServer.Database.Models.Abstract.Interfaces;

namespace AuthServer.Database.Models.Interfaces;

public interface ILocalUser : IUser
{
    byte[] PasswordHash { get; set; }
    byte[] SaltHash { get; set; }
    DateTime PasswordUpdatedAt { get; set; }
    ICollection<LocalUserRefreshToken>? RefreshTokens { get; set; }
}