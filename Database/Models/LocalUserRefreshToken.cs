using AuthServer.Database.Enums;
using AuthServer.Database.Models.Interfaces;

namespace AuthServer.Database.Models;

public sealed class LocalUserRefreshToken : UserRefreshToken, ILocalUserRefreshToken
{
    public LocalUser? User { get; set; }
    public EUserAuthScope Scopes { get; set; }
}