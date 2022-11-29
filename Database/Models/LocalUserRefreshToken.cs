using AuthServer.Database.Models.Abstract;
using AuthServer.Database.Models.Interfaces;

namespace AuthServer.Database.Models;

public sealed class LocalUserRefreshToken : UserRefreshToken, ILocalUserRefreshToken
{
    public LocalUser? User { get; set; }
    public int Scopes { get; set; }
}