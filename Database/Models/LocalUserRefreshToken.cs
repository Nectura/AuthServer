using AuthServer.Database.Models.Abstract;

namespace AuthServer.Database.Models;

public sealed class LocalUserRefreshToken : UserRefreshToken
{
    public LocalUser? User { get; set; }
    public int Scopes { get; set; }
}