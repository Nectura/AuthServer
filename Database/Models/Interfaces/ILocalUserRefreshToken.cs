using AuthServer.Database.Models.Abstract.Interfaces;

namespace AuthServer.Database.Models.Interfaces;

public interface ILocalUserRefreshToken : IUserRefreshToken
{
    LocalUser? User { get; set; }
    int Scopes { get; set; }
}