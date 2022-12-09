using AuthServer.Database.Models.Abstract.Interfaces;

namespace AuthServer.Database.Models.Interfaces;

public interface ILocalUser : IUser
{
    byte[] PasswordHash { get; set; }
    byte[] SaltHash { get; set; }
    string? ProfilePicture { get; set; }
}